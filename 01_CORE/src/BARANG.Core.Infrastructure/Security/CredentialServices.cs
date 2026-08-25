using System.Security.Cryptography;
using BARANG.Core.Application.Security;
using BARANG.Core.Domain.Common;

namespace BARANG.Core.Infrastructure.Security;

public sealed class PasswordCredentialService : IPasswordCredentialService
{
    private const int Iterations = 210_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private static readonly string DummyHash = CreateHash("BARANG-dummy-password", new byte[SaltSize]);

    public Result<string> HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 12 || password.Length > 256 || !password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit)) return Result.Failure<string>(BARANG.Core.Domain.Security.SecurityErrors.InvalidPassword);
        return Result.Success(CreateHash(password, RandomNumberGenerator.GetBytes(SaltSize)));
    }

    public bool Verify(string password, string? hash)
    {
        string encoded = hash ?? DummyHash;
        string[] parts = encoded.Split('.');
        if (parts.Length != 4 || parts[0] != "v1" || !int.TryParse(parts[1], out int iterations)) return false;
        try { byte[] salt = Convert.FromBase64String(parts[2]); byte[] expected = Convert.FromBase64String(parts[3]); byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length); return CryptographicOperations.FixedTimeEquals(actual, expected) && hash is not null; }
        catch (FormatException) { return false; }
    }
    private static string CreateHash(string password, byte[] salt) => $"v1.{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashSize))}";
}

public sealed class SessionTokenService : ISessionTokenService
{
    public (string Token, string Hash) Create() { string token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_'); return (token, Hash(token)); }
    public string Hash(string token) => Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));
}
