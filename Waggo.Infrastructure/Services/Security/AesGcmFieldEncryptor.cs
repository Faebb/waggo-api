using System.Security.Cryptography;
using System.Text;
using Waggo.Infrastructure.Options.Security;

namespace Waggo.Infrastructure.Services.Security;

/// <summary>
/// Encrypts sensitive columns with AES-256-GCM (RNF-003). Output: <c>v1:</c> + base64(nonce | tag | cipher).
/// A random nonce per value means the same text never produces the same cipher; the tag detects tampering.
/// </summary>
public sealed class AesGcmFieldEncryptor
{
    public const string Prefix = "v1:";

    private const int KeySize = 32;
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[] _key;

    public AesGcmFieldEncryptor(EncryptionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _key = ParseKey(options.Key);
    }

    public string Encrypt(string plainText)
    {
        ArgumentNullException.ThrowIfNull(plainText);

        byte[] plain = Encoding.UTF8.GetBytes(plainText);
        byte[] output = new byte[NonceSize + TagSize + plain.Length];
        Span<byte> nonce = output.AsSpan(0, NonceSize);
        Span<byte> tag = output.AsSpan(NonceSize, TagSize);
        Span<byte> cipher = output.AsSpan(NonceSize + TagSize);

        RandomNumberGenerator.Fill(nonce);
        using AesGcm aes = new(_key, TagSize);
        aes.Encrypt(nonce, plain, cipher, tag);

        return Prefix + Convert.ToBase64String(output);
    }

    public string Decrypt(string cipherText)
    {
        ArgumentNullException.ThrowIfNull(cipherText);
        if (!cipherText.StartsWith(Prefix, StringComparison.Ordinal))
        {
            throw new CryptographicException("The value was not encrypted by AesGcmFieldEncryptor.");
        }

        byte[] input = Convert.FromBase64String(cipherText[Prefix.Length..]);
        if (input.Length < NonceSize + TagSize)
        {
            throw new CryptographicException("The encrypted value is too short.");
        }

        ReadOnlySpan<byte> nonce = input.AsSpan(0, NonceSize);
        ReadOnlySpan<byte> tag = input.AsSpan(NonceSize, TagSize);
        ReadOnlySpan<byte> cipher = input.AsSpan(NonceSize + TagSize);
        byte[] plain = new byte[cipher.Length];

        using AesGcm aes = new(_key, TagSize);
        aes.Decrypt(nonce, cipher, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }

    private static byte[] ParseKey(string key)
    {
        byte[] bytes = new byte[KeySize + 1];
        if (!Convert.TryFromBase64String(key ?? string.Empty, bytes, out int written) || written != KeySize)
        {
            throw new InvalidOperationException(
                "Encryption:Key must be 32 random bytes in base64 (generate one with: openssl rand -base64 32).");
        }

        return bytes[..KeySize];
    }
}
