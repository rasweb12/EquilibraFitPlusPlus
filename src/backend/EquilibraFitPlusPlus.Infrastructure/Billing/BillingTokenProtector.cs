using System.Security.Cryptography;
using System.Text;

namespace EquilibraFitPlusPlus.Infrastructure.Billing;

public sealed class BillingTokenProtector(GooglePlayOptions options)
{
    public static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    public static string AccountId(Guid userId) => Hash(userId.ToString("D"));

    public string Encrypt(string token)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plaintext = Encoding.UTF8.GetBytes(token);
        var encrypted = new byte[plaintext.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(Key(), 16);
        aes.Encrypt(nonce, plaintext, encrypted, tag);
        return Convert.ToBase64String([.. nonce, .. tag, .. encrypted]);
    }
    public string Decrypt(string value)
    {
        var bytes = Convert.FromBase64String(value);
        if (bytes.Length < 28) throw new CryptographicException("Invalid protected billing token.");
        var plaintext = new byte[bytes.Length - 28];
        using var aes = new AesGcm(Key(), 16);
        aes.Decrypt(bytes.AsSpan(0, 12), bytes.AsSpan(28), bytes.AsSpan(12, 16), plaintext);
        return Encoding.UTF8.GetString(plaintext);
    }
    private byte[] Key()
    {
        var key = Convert.FromBase64String(options.TokenEncryptionKey);
        if (key.Length != 32) throw new InvalidOperationException("BILLING_TOKEN_ENCRYPTION_KEY must contain 32 random bytes in base64.");
        return key;
    }
}
