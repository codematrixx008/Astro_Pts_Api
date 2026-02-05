using Astro.Application.Auth;
using Astro.Application.ApiKeys;
using Astro.Application.Billing;
using Astro.Application.Common;
using Astro.Application.Ephemeris;
using Astro.Application.Security;
using Astro.Domain.Auth;
using Astro.Domain.ApiUsage;
using Astro.Domain.Billing;
using Astro.Domain.Chat;
using Astro.Domain.Marketplace;
using Astro.Infrastructure.Data;
using Astro.Infrastructure.Repositories;
using Astro.Api.Authorization;
using Astro.Api.Hubs;
using Astro.Api.Middleware;
using Astro.Api.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Astro.Domain.Consumers;

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
var builder = WebApplication.CreateBuilder(args);

// =====================================================
// Configuration
// =====================================================
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<AuthCookieOptions>(builder.Configuration.GetSection("AuthCookies"));

var dbOpts = new DbOptions();
builder.Configuration.GetSection("Db").Bind(dbOpts);
builder.Services.AddSingleton(dbOpts);
builder.Services.AddSingleton<IDbConnectionFactory>(_ => new DbConnectionFactory(dbOpts));
builder.Services.AddSingleton<DbInitializer>();

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton(new Pbkdf2Hasher());

// Deterministic refresh-token hashing (sessions)
builder.Services.AddSingleton(sp =>
{
    var key = builder.Configuration["RefreshTokens:HashKey"]
        ?? throw new Exception("RefreshTokens:HashKey missing.");
    return new RefreshTokenHasher(key);
});

// CORS for React UI (cookie auth needs AllowCredentials)
//var uiOrigin = builder.Configuration["Cors:UiOrigin"] ?? "http://localhost:3000";

//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("ui", policy =>
//    {
//        policy.WithOrigins(
//              "http://localhost:5173",
//              "http://localhost:3000"
//        )
//              .AllowAnyHeader()
//              .AllowAnyMethod()
//              .AllowCredentials();
//    });
//});

builder.Services.AddCors(options =>
{
    options.AddPolicy(MyAllowSpecificOrigins, policy =>
    {
        policy
            // Explicit origins (SignalR friendly)
            .WithOrigins(
                "http://localhost:5173",
                "http://localhost:3000",
                "https://localhost:51790",
                "http://103.119.198.238"
            )

            // Allow LAN + dynamic
            .SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrWhiteSpace(origin))
                    return false;

                try
                {
                    var uri = new Uri(origin);

                    return uri.Host.StartsWith("192.168.")
                        || uri.Host == "localhost"
                        || uri.Host == "103.119.198.238";
                }
                catch
                {
                    return false;
                }
            })

            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});


// =====================================================
// Repositories (Dapper)
// =====================================================
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();
builder.Services.AddScoped<IUserOrganizationRepository, UserOrganizationRepository>();

// Multi-role + sessions
builder.Services.AddScoped<IUserRoleRepository, UserRoleRepository>();
builder.Services.AddScoped<IUserSessionRepository, UserSessionRepository>();

// Legacy refresh token table (kept for compatibility; not used by cookie-session auth)
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

// API key product + usage
builder.Services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
builder.Services.AddScoped<IApiUsageLogRepository, ApiUsageLogRepository>();
builder.Services.AddScoped<IApiUsageCounterRepository, ApiUsageCounterRepository>();

// Marketplace
builder.Services.AddScoped<IAstrologerProfileRepository, AstrologerProfileRepository>();
builder.Services.AddScoped<IAstrologerAvailabilityRepository, AstrologerAvailabilityRepository>();

// Chat
builder.Services.AddScoped<IChatSessionRepository, ChatSessionRepository>();
builder.Services.AddScoped<IChatMessageRepository, ChatMessageRepository>();

// Billing / Ledger
builder.Services.AddScoped<ILedgerRepository, LedgerRepository>();
builder.Services.AddScoped<IPayoutRepository, PayoutRepository>();

//Cosumer
builder.Services.AddScoped<IConsumerProfileRepository, ConsumerProfileRepository>();

// =====================================================
// Application Services
// =====================================================
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ApiKeyService>();
builder.Services.AddScoped<BillingService>();

builder.Services.AddSingleton<IEphemerisService, PlaceholderEphemerisService>();

// SignalR
builder.Services.AddSignalR();

// =====================================================
// Auth (JWT for portal endpoints)
// =====================================================
var jwtSection = builder.Configuration.GetSection("Jwt");
var signingKey = jwtSection.GetValue<string>("SigningKey") ?? throw new Exception("Jwt:SigningKey missing.");
var issuer = jwtSection.GetValue<string>("Issuer") ?? "Astro.Api";
var audience = jwtSection.GetValue<string>("Audience") ?? "Astro.Api";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.RequireHttpsMetadata = false; // dev only
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = issuer,
            ValidAudience = audience,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.NameIdentifier
        };

        // Allow SignalR access tokens over query string: /hubs/chat?access_token=...
        opts.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var path = context.HttpContext.Request.Path;
                var accessToken = context.Request.Query["access_token"].ToString();
                if (!string.IsNullOrWhiteSpace(accessToken) && path.StartsWithSegments("/hubs/chat"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        ScopePolicies.EphemerisRead,
        policy => policy.Requirements.Add(
            new ScopeRequirement(ScopePolicies.EphemerisRead)
        )
    );
});

builder.Services.AddSingleton<IAuthorizationHandler, ScopeAuthorizationHandler>();


// =====================================================
// Rate limiting (partition by API key id)
// =====================================================
var permitLimit = builder.Configuration.GetSection("RateLimiting").GetValue<int>("PermitLimitPerMinute", 60);

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("api", httpContext =>
    {
        var apiCtx = httpContext.GetApiKeyContext();
        var partitionKey = apiCtx?.ApiKeyId.ToString() ?? "anon";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: partitionKey,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// =====================================================
// MVC + Swagger
// =====================================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Astro API Foundation", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme."
    });

    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        Name = "X-Api-Key",
        In = ParameterLocation.Header,
        Description = "API key header. Format: <prefix>.<secret>"
    });

    c.OperationFilter<Astro.Api.Swagger.SecurityRequirementsOperationFilter>();

});

var app = builder.Build();

// Initialize DB
using (var scope = app.Services.CreateScope())
{
    var init = scope.ServiceProvider.GetRequiredService<DbInitializer>();
    await init.InitializeAsync(CancellationToken.None);
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseRouting();
app.UseCors(MyAllowSpecificOrigins);

app.UseCookiePolicy(new CookiePolicyOptions
{
    MinimumSameSitePolicy = SameSiteMode.None
});

app.UseHttpsRedirection();

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Rate limiter early
app.UseRateLimiter();

// Usage logging should see the final status code
app.UseMiddleware<ApiUsageLoggingMiddleware>();

app.UseAuthentication();

// API Key auth for /v1/* endpoints (machine access)
app.UseWhen(ctx => ctx.Request.Path.StartsWithSegments("/v1"), appBuilder =>
{
    appBuilder.UseMiddleware<ApiKeyAuthMiddleware>();
    appBuilder.UseMiddleware<ApiQuotaMiddleware>();
});

app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

app.MapGet("/health", () => Results.Ok(new { ok = true, utc = DateTime.UtcNow })).AllowAnonymous();

app.Run();
