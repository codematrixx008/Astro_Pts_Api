using System.Security.Cryptography;

namespace Astro.Application.Security;

public static class TokenGenerator
{
    // URL-safe base64 without padding (base64url)
    public static string CreateSecret(int bytes = 32)
    {
        var data = RandomNumberGenerator.GetBytes(bytes);
        return Base64UrlEncode(data);
    }

    public static string CreatePrefix(string prefix = "ak_live_", int suffixBytes = 6)
        => $"{prefix}{Base64UrlEncode(RandomNumberGenerator.GetBytes(suffixBytes))}";

    private static string Base64UrlEncode(byte[] data)
        => Convert.ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
}
