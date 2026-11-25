using System.Security.Cryptography;
using System.Text;

namespace BARANG.Core.Infrastructure.Ai;

/// <summary>رمزنگاری کلید API provider با AES-GCM.
/// کلید از تنظیمات Ai:SettingsEncryptionKey (یا مقدار پیش‌فرض توسعه) مشتق می‌شود تا پس از ری‌استارت کانتینر رمزگشایی ممکن بماند.</summary>
public static class AiSecretCipher
{
    private const string DevelopmentFallbackKey = "barang-ai-settings-dev-key-v1";

    public static string Encrypt(string plainText, string? configuredKey)
    {
        byte[] key = DeriveKey(configuredKey);
        byte[] nonce = RandomNumberGenerator.GetBytes(12);
        byte[] cipher = AesGcmEncrypt(key, nonce, Encoding.UTF8.GetBytes(plainText));
        byte[] blob = [.. nonce, .. cipher];
        return Convert.ToBase64String(blob);
    }

    public static string? TryDecrypt(string? cipherText, string? configuredKey)
    {
        if (string.IsNullOrWhiteSpace(cipherText)) return null;
        try
        {
            byte[] blob = Convert.FromBase64String(cipherText);
            if (blob.Length < 12 + 16) return null;
            byte[] nonce = blob[..12];
            byte[] cipher = blob[12..];
            byte[] plain = AesGcmDecrypt(DeriveKey(configuredKey), nonce, cipher);
            return Encoding.UTF8.GetString(plain);
        }
        catch (CryptographicException)
        {
            return null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    public static string Mask(string secret)
    {
        string trimmed = secret.Trim();
        if (trimmed.Length <= 4) return "••••";
        return "••••••••" + trimmed[^4..];
    }

    private static byte[] AesGcmEncrypt(byte[] key, byte[] nonce, byte[] plain)
    {
        byte[] cipher = new byte[plain.Length];
        byte[] tag = new byte[16];
        using AesGcm aes = new(key, tagSizeInBytes: 16);
        aes.Encrypt(nonce, plain, cipher, tag);
        return [.. cipher, .. tag];
    }

    private static byte[] AesGcmDecrypt(byte[] key, byte[] nonce, byte[] cipherWithTag)
    {
        byte[] tag = cipherWithTag[^16..];
        byte[] cipher = cipherWithTag[..^16];
        byte[] plain = new byte[cipher.Length];
        using AesGcm aes = new(key, tagSizeInBytes: 16);
        aes.Decrypt(nonce, cipher, tag, plain);
        return plain;
    }

    private static byte[] DeriveKey(string? configuredKey)
    {
        string source = string.IsNullOrWhiteSpace(configuredKey) ? DevelopmentFallbackKey : configuredKey;
        return SHA256.HashData(Encoding.UTF8.GetBytes(source));
    }
}
