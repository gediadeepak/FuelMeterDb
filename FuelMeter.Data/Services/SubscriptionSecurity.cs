using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace FuelMeter.Data.Services;

public sealed class SubscriptionSecurity(IConfiguration configuration)
{
    private byte[] Key(string name)
    {
        var value = configuration["SubscriptionSecurity:" + name];
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"SubscriptionSecurity:{name} is missing.");
        var key = Convert.FromBase64String(value);
        if (key.Length != 32) throw new InvalidOperationException($"SubscriptionSecurity:{name} must be a 256-bit key.");
        return key;
    }

    public string AccountId(int userId) => Convert.ToHexString(
        HMACSHA256.HashData(Key("IdentityKey"), Encoding.UTF8.GetBytes("account:" + userId)));

    public string TrialIdentity(string email) => Convert.ToHexString(
        HMACSHA256.HashData(Key("IdentityKey"), Encoding.UTF8.GetBytes("trial:" + email.Trim().ToLowerInvariant())));

    public static string TokenHash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    public string EncryptToken(string token)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plaintext = Encoding.UTF8.GetBytes(token);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(Key("PurchaseTokenKey"), 16);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);
        return Convert.ToBase64String([.. nonce, .. tag, .. ciphertext]);
    }

    public string DecryptToken(string token)
    {
        var bytes = Convert.FromBase64String(token);
        if (bytes.Length < 28) throw new CryptographicException("Invalid encrypted purchase reference.");
        var plaintext = new byte[bytes.Length - 28];
        using var aes = new AesGcm(Key("PurchaseTokenKey"), 16);
        aes.Decrypt(bytes.AsSpan(0, 12), bytes.AsSpan(28), bytes.AsSpan(12, 16), plaintext);
        return Encoding.UTF8.GetString(plaintext);
    }
}
