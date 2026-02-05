namespace Astro.Domain.Auth;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);
    Task<User?> GetByIdAsync(long userId, CancellationToken ct);
    Task<long> CreateAsync(string email, string passwordHash, CancellationToken ct);
}

public interface IOrganizationRepository
{
    Task<long> CreateAsync(string name, CancellationToken ct);
    Task<Organization?> GetByIdAsync(long orgId, CancellationToken ct);
}

public interface IUserOrganizationRepository
{
    Task AddOwnerAsync(long userId, long orgId, CancellationToken ct);
    Task<IReadOnlyList<UserOrganization>> GetForUserAsync(long userId, CancellationToken ct);
}

public interface IRefreshTokenRepository
{
    Task<long> CreateAsync(long userId, string tokenHash, DateTime expiresUtc, CancellationToken ct);
    Task<RefreshToken?> GetValidByPlainAsync(long userId, string refreshTokenPlain, DateTime nowUtc, CancellationToken ct);
    Task RevokeAsync(long refreshTokenId, DateTime revokedUtc, string? replacedByTokenHash, CancellationToken ct);
}

public interface IApiKeyRepository
{
    Task<long> CreateAsync(long orgId, string name, string prefix, string secretHash, string scopesCsv, int? dailyQuota, string? planCode, CancellationToken ct);
    Task<IReadOnlyList<ApiKey>> ListAsync(long orgId, CancellationToken ct);
    Task<ApiKey?> GetActiveByPrefixAsync(string prefix, CancellationToken ct);
    Task RevokeAsync(long apiKeyId, DateTime revokedUtc, CancellationToken ct);
    Task TouchLastUsedAsync(long apiKeyId, DateTime lastUsedUtc, CancellationToken ct);
}

public interface IApiUsageLogRepository
{
    Task CreateAsync(ApiUsageLog log, CancellationToken ct);
}
