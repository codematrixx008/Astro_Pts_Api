using System.Security.Cryptography;
using System.Text;

namespace Astro.Application.Security;

/// <summary>
/// Deterministic HMAC hash for refresh tokens so we can look up sessions by hash.
/// </summary>
public sealed class RefreshTokenHasher
{
    private readonly byte[] _key;

    public RefreshTokenHasher(string hashKey)
    {
        _key = Encoding.UTF8.GetBytes(hashKey);
        if (_key.Length < 32)
            throw new ArgumentException("RefreshTokens:HashKey must be at least 32 chars.");
    }

    public string Hash(string refreshTokenPlain)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenPlain))
            return string.Empty;

        using var h = new HMACSHA256(_key);
        var bytes = h.ComputeHash(Encoding.UTF8.GetBytes(refreshTokenPlain));
        return Convert.ToBase64String(bytes);
    }

}
