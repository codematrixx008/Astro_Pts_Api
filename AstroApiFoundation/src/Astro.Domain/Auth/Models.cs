namespace Astro.Domain.Auth;

public sealed record User(
    long UserId,
    string Email,
    string PasswordHash,
    DateTime CreatedUtc,
    bool IsActive
);

public sealed record Organization(
    long OrgId,
    string Name,
    DateTime CreatedUtc,
    bool IsActive
);

public sealed record UserOrganization(
    long UserId,
    long OrgId,
    string Role // "Owner", "Admin", "Member"
);

public sealed record RefreshToken(
    long RefreshTokenId,
    long UserId,
    string TokenHash,
    DateTime ExpiresUtc,
    DateTime CreatedUtc,
    DateTime? RevokedUtc,
    string? ReplacedByTokenHash
);

//public sealed record ApiKey(
//    long ApiKeyId,
//    long OrgId,
//    string Name,
//    string Prefix,
//    string SecretHash,
//    string ScopesCsv,
//    bool IsActive,
//    DateTime CreatedUtc,
//    DateTime? LastUsedUtc,
//    DateTime? RevokedUtc,
//    int? DailyQuota,     
//    string? PlanCode     
//);
public sealed class ApiKey
{
    // Required by Dapper
    public ApiKey() { }

    public long ApiKeyId { get; init; }
    public long OrgId { get; init; }
    public string Name { get; init; } = default!;
    public string Prefix { get; init; } = default!;
    public string SecretHash { get; init; } = default!;
    public string ScopesCsv { get; init; } = default!;
    public bool IsActive { get; init; }
    public DateTime CreatedUtc { get; init; }
    public DateTime? LastUsedUtc { get; init; }
    public DateTime? RevokedUtc { get; init; }

    // Optional / nullable
    public int? DailyQuota { get; init; }
    public string? PlanCode { get; init; }
}
public sealed record ApiUsageLog(
    long ApiUsageLogId,
    long? ApiKeyId,
    long? UserId,
    string Method,
    string Path,
    int StatusCode,
    long DurationMs,
    string? Ip,
    DateTime CreatedUtc
);
