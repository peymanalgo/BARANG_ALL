using System.Security.Cryptography;
using BARANG.Core.Application.Security;
using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Security;
using BARANG.Core.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BARANG.Core.Infrastructure.Security;

public sealed class LegacyCompatiblePasswordHasher : IPasswordHasher<ApplicationUser>
{
    private const int LegacyIterations = 210_000;
    private const int LegacySaltSize = 16;
    private const int LegacyHashSize = 32;
    private readonly PasswordHasher<ApplicationUser> identityHasher = new();
    private static readonly string DummyHash = CreateLegacyHash("BARANG-dummy-password", new byte[LegacySaltSize]);

    public string HashPassword(ApplicationUser user, string password) => identityHasher.HashPassword(user, password);

    public PasswordVerificationResult VerifyHashedPassword(ApplicationUser user, string hashedPassword, string providedPassword)
    {
        if (hashedPassword.StartsWith("AQAAAA", StringComparison.Ordinal))
            return identityHasher.VerifyHashedPassword(user, hashedPassword, providedPassword);

        bool verified = VerifyLegacy(providedPassword, string.IsNullOrWhiteSpace(hashedPassword) ? DummyHash : hashedPassword);
        return verified && !string.IsNullOrWhiteSpace(hashedPassword) ? PasswordVerificationResult.SuccessRehashNeeded : PasswordVerificationResult.Failed;
    }

    internal static bool VerifyLegacy(string password, string encoded)
    {
        string[] parts = encoded.Split('.');
        if (parts.Length != 4 || parts[0] != "v1" || !int.TryParse(parts[1], out int iterations)) return false;
        try
        {
            byte[] salt = Convert.FromBase64String(parts[2]);
            byte[] expected = Convert.FromBase64String(parts[3]);
            byte[] actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException) { return false; }
    }

    private static string CreateLegacyHash(string password, byte[] salt) => $"v1.{LegacyIterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(Rfc2898DeriveBytes.Pbkdf2(password, salt, LegacyIterations, HashAlgorithmName.SHA256, LegacyHashSize))}";
}

// Compatibility adapter used by legacy bootstrap tests and callers. New account workflows use UserManager through IdentityAccountService.
public sealed class PasswordCredentialService(IPasswordHasher<ApplicationUser> hasher) : IPasswordCredentialService
{
    public PasswordCredentialService() : this(new LegacyCompatiblePasswordHasher()) { }

    public Result<string> HashPassword(string password)
    {
        if (!PasswordPolicy.IsValid(password)) return Result.Failure<string>(SecurityErrors.InvalidPassword);
        ApplicationUser subject = ApplicationUser.Create(Guid.NewGuid(), "HASH.SUBJECT", "Hash subject", null, "placeholder", DateTimeOffset.UnixEpoch).Value;
        return Result.Success(hasher.HashPassword(subject, password));
    }

    public bool Verify(string password, string? hash)
    {
        ApplicationUser subject = ApplicationUser.Create(Guid.NewGuid(), "HASH.SUBJECT", "Hash subject", null, "placeholder", DateTimeOffset.UnixEpoch).Value;
        string value = hash ?? string.Empty;
        return hasher.VerifyHashedPassword(subject, value, password) != PasswordVerificationResult.Failed && hash is not null;
    }
}

internal static class PasswordPolicy
{
    public static bool IsValid(string password) => !string.IsNullOrWhiteSpace(password) && password.Length is >= 12 and <= 256 && password.Any(char.IsUpper) && password.Any(char.IsLower) && password.Any(char.IsDigit);
}

public sealed class BarangIdentityUserStore(CoreDbContext db, TimeProvider time) :
    IUserPasswordStore<ApplicationUser>, IUserLockoutStore<ApplicationUser>, IUserSecurityStampStore<ApplicationUser>
{
    public void Dispose() { }
    public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.Id.ToString());
    public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult<string?>(user.Username);
    public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken) { user.SetUsername(userName ?? string.Empty, time.GetUtcNow()); return Task.CompletedTask; }
    public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult<string?>(user.Username);
    public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken) { db.ApplicationUsers.Add(user); return Task.FromResult(IdentityResult.Success); }
    public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
    public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Failed(new IdentityError { Code = "UserDeletionDisabled", Description = "User accounts are retained." }));
    public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) => Guid.TryParse(userId, out Guid id) ? db.ApplicationUsers.SingleOrDefaultAsync(x => x.Id == id, cancellationToken) : Task.FromResult<ApplicationUser?>(null);
    public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        string normalized = normalizedUserName.Trim().ToUpperInvariant();
        return db.ApplicationUsers.SingleOrDefaultAsync(x => x.Username == normalized, cancellationToken);
    }

    public Task SetPasswordHashAsync(ApplicationUser user, string? passwordHash, CancellationToken cancellationToken) { if (!string.IsNullOrWhiteSpace(passwordHash)) user.SetPasswordHash(passwordHash, time.GetUtcNow()); return Task.CompletedTask; }
    public Task<string?> GetPasswordHashAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult<string?>(user.PasswordHash);
    public Task<bool> HasPasswordAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(!string.IsNullOrWhiteSpace(user.PasswordHash));
    public Task SetSecurityStampAsync(ApplicationUser user, string stamp, CancellationToken cancellationToken) { user.SetSecurityStamp(stamp, time.GetUtcNow()); return Task.CompletedTask; }
    public Task<string?> GetSecurityStampAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult<string?>(user.SecurityStamp);

    public Task<DateTimeOffset?> GetLockoutEndDateAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.LockoutEndUtc);
    public Task SetLockoutEndDateAsync(ApplicationUser user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken) { user.SetLockoutEnd(lockoutEnd, time.GetUtcNow()); return Task.CompletedTask; }
    public Task<int> IncrementAccessFailedCountAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.IncrementAccessFailed(time.GetUtcNow()));
    public Task ResetAccessFailedCountAsync(ApplicationUser user, CancellationToken cancellationToken) { user.ResetAccessFailed(time.GetUtcNow()); return Task.CompletedTask; }
    public Task<int> GetAccessFailedCountAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(user.AccessFailedCount);
    public Task<bool> GetLockoutEnabledAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(true);
    public Task SetLockoutEnabledAsync(ApplicationUser user, bool enabled, CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class IdentityAccountService(UserManager<ApplicationUser> users) : IIdentityAccountService
{
    public async Task<Result> CreateAsync(ApplicationUser user, string password, CancellationToken ct) => Map(await users.CreateAsync(user, password));

    public async Task<Result> UpdateUsernameAsync(ApplicationUser user, string username, CancellationToken ct)
    {
        IdentityResult result = await users.SetUserNameAsync(user, username);
        if (!result.Succeeded) return Map(result);
        return string.Equals(user.Username, username.Trim(), StringComparison.OrdinalIgnoreCase) ? Result.Success() : Result.Failure(SecurityErrors.InvalidIdentity);
    }

    public async Task<Result> ResetPasswordAsync(ApplicationUser user, string newPassword, CancellationToken ct)
    {
        string token = await users.GeneratePasswordResetTokenAsync(user);
        return Map(await users.ResetPasswordAsync(user, token, newPassword));
    }

    public async Task<Result<PasswordAuthenticationOutcome>> CheckPasswordAsync(ApplicationUser user, string password, CancellationToken ct)
    {
        if (user.AccountState is UserAccountState.Inactive or UserAccountState.Locked) return Result.Success(PasswordAuthenticationOutcome.Failed);
        if (await users.IsLockedOutAsync(user)) return Result.Success(PasswordAuthenticationOutcome.Locked);
        if (await users.CheckPasswordAsync(user, password))
        {
            await users.ResetAccessFailedCountAsync(user);
            return Result.Success(PasswordAuthenticationOutcome.Success);
        }
        await users.AccessFailedAsync(user);
        return Result.Success(await users.IsLockedOutAsync(user) ? PasswordAuthenticationOutcome.Locked : PasswordAuthenticationOutcome.Failed);
    }

    public async Task<Result> UnlockAsync(ApplicationUser user, CancellationToken ct)
    {
        IdentityResult unlocked = await users.SetLockoutEndDateAsync(user, null);
        if (!unlocked.Succeeded) return Map(unlocked);
        return Map(await users.ResetAccessFailedCountAsync(user));
    }

    private static Result Map(IdentityResult result)
    {
        if (result.Succeeded) return Result.Success();
        if (result.Errors.Any(x => x.Code.Contains("DuplicateUserName", StringComparison.OrdinalIgnoreCase))) return Result.Failure(SecurityErrors.Conflict);
        if (result.Errors.Any(x => x.Code.StartsWith("Password", StringComparison.OrdinalIgnoreCase))) return Result.Failure(SecurityErrors.InvalidPassword);
        return Result.Failure(SecurityErrors.InvalidIdentity);
    }
}

public sealed class SessionTokenService : ISessionTokenService
{
    public (string Token, string Hash) Create() { string token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_'); return (token, Hash(token)); }
    public string Hash(string token) => Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));
}
