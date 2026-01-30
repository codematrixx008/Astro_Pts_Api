using Astro.Application.Auth;
using Astro.Application.ApiKeys;
using Astro.Application.Common;
using Astro.Application.Ephemeris;
using Astro.Application.Security;
using Astro.Domain.Auth;
using Astro.Infrastructure.Data;
using Astro.Infrastructure.Repositories;
using Astro.Api.Security;
using Astro.Api.Middleware;
using Astro.Api.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Threading.RateLimiting;
using Astro.Domain.Interface;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// Configuration
// =====================================================
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection("Jwt"));

var dbOptions = new DbOptions();
builder.Configuration.GetSection("Db").Bind(dbOptions);

builder.Services.AddSingleton(dbOptions);
builder.Services.AddSingleton<IDbConnectionFactory>(
    _ => new DbConnectionFactory(dbOptions));

builder.Services.AddSingleton<DbInitializer>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton(new Pbkdf2Hasher());

// =====================================================
// Repositories (Dapper)
// =====================================================
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();
builder.Services.AddScoped<IUserOrganizationRepository, UserOrganizationRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
builder.Services.AddScoped<IApiUsageLogRepository, ApiUsageLogRepository>();
builder.Services.AddScoped<IFavourablePointRepository, FavourablePointRepository>();


// =====================================================
// Application Services
// =====================================================
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ApiKeyService>();
builder.Services.AddSingleton<IEphemerisService, PlaceholderEphemerisService>();

// =====================================================
// Authentication (JWT)
// =====================================================
var jwtSection = builder.Configuration.GetSection("Jwt");
var signingKey = jwtSection.GetValue<string>("SigningKey")
    ?? throw new Exception("Jwt:SigningKey missing");

var issuer = jwtSection.GetValue<string>("Issuer") ?? "Astro.Api";
var audience = jwtSection.GetValue<string>("Audience") ?? "Astro.Api";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // dev only
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = issuer,
            ValidAudience = audience,
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(signingKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

// =====================================================
// Authorization
// =====================================================
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(ScopePolicies.EphemerisRead, policy =>
        policy.Requirements.Add(
            new ScopeRequirement("ephemeris.read")));
});

builder.Services.AddSingleton<
    Microsoft.AspNetCore.Authorization.IAuthorizationHandler,
    ScopeAuthorizationHandler>();

// =====================================================
// Rate Limiting
// =====================================================
var permitLimit = builder.Configuration
    .GetSection("RateLimiting")
    .GetValue<int>("PermitLimitPerMinute", 60);

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("api", context =>
    {
        var apiContext = context.GetApiKeyContext();
        var key = apiContext?.ApiKeyId.ToString() ?? "anonymous";

        return RateLimitPartition.GetFixedWindowLimiter(
            key,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
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
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Astro API Foundation",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.ApiKey,
        Name = "X-Api-Key",
        In = ParameterLocation.Header,
        Description = "API Key header"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        },
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// =====================================================
// Database Init
// =====================================================
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider
        .GetRequiredService<DbInitializer>();

    await initializer.InitializeAsync(CancellationToken.None);
}

// =====================================================
// 🚀 AUTO REDIRECT ROOT → SWAGGER
// =====================================================
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
        context.Response.Redirect("/swagger/index.html");
        return;
    }
    await next();
});

// =====================================================
// Middleware Pipeline
// =====================================================
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

// Rate limiter early
app.UseRateLimiter();

app.UseMiddleware<ApiUsageLoggingMiddleware>();

app.UseAuthentication();

app.UseWhen(
    ctx => ctx.Request.Path.StartsWithSegments("/v1"),
    appBuilder =>
    {
        appBuilder.UseAuthentication();
        appBuilder.UseMiddleware<ApiKeyAuthMiddleware>();
        appBuilder.UseAuthorization();
    });

app.UseAuthorization();

app.MapControllers();

app.MapGet("/health", () =>
    Results.Ok(new
    {
        ok = true,
        utc = DateTime.UtcNow
    }))
    .AllowAnonymous();

app.Run();
