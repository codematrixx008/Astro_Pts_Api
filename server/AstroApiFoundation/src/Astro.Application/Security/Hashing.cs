using System.Security.Cryptography;

namespace Astro.Application.Security;

/// <summary>
/// PBKDF2 hasher for passwords/secrets. Stored format: {iterations}.{saltB64}.{hashB64}
/// </summary>
public sealed class Pbkdf2Hasher
{
    private readonly int _iterations;
    private readonly int _saltSizeBytes;
    private readonly int _hashSizeBytes;

    public Pbkdf2Hasher(int iterations = 150_000, int saltSizeBytes = 16, int hashSizeBytes = 32)
    {
        _iterations = iterations;
        _saltSizeBytes = saltSizeBytes;
        _hashSizeBytes = hashSizeBytes;
    }

    public string Hash(string secret)
    {
        var salt = RandomNumberGenerator.GetBytes(_saltSizeBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password: secret,
            salt: salt,
            iterations: _iterations,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: _hashSizeBytes);

        return $"{_iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string secret, string stored)
    {
        if (string.IsNullOrWhiteSpace(stored)) return false;

        var parts = stored.Split('.', 3);
        if (parts.Length != 3) return false;

        if (!int.TryParse(parts[0], out var iterations)) return false;

        var salt = Convert.FromBase64String(parts[1]);
        var expected = Convert.FromBase64String(parts[2]);

        var actual = Rfc2898DeriveBytes.Pbkdf2(
            password: secret,
            salt: salt,
            iterations: iterations,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: expected.Length);

        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
