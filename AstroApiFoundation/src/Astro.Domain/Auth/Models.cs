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

// External login mapping (e.g. Google 'sub' -> local UserId)
public sealed record ExternalIdentity(
    long ExternalIdentityId,
    long UserId,
    string Provider,
    string ProviderUserId,
    string EmailSnapshot,
    DateTime CreatedUtc
);

public sealed record ApiKey(
    long ApiKeyId,
    long OrgId,
    string Name,
    string Prefix,
    string SecretHash,
    string ScopesCsv,
    bool IsActive,
    DateTime CreatedUtc,
    DateTime? LastUsedUtc,
    DateTime? RevokedUtc,
    int? DailyQuota,
    string? PlanCode
);

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
