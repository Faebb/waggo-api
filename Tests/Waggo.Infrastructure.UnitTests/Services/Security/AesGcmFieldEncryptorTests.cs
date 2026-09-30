using System.Security.Cryptography;
using Waggo.Infrastructure.Options.Security;
using Waggo.Infrastructure.Services.Security;

namespace Waggo.Infrastructure.UnitTests.Services.Security;

/// <summary>RNF-003: sensitive columns are encrypted with AES-256-GCM before they reach the database.</summary>
public class AesGcmFieldEncryptorTests
{
    private static readonly string s_key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private readonly AesGcmFieldEncryptor _sut = new(new EncryptionOptions { Key = s_key });

    [Fact]
    public void Encrypt_ThenDecrypt_ReturnsTheOriginalText()
    {
        string cipher = _sut.Encrypt("Alérgica al pollo");

        _sut.Decrypt(cipher).ShouldBe("Alérgica al pollo");
    }

    [Fact]
    public void Encrypt_NeverContainsThePlainText()
    {
        string cipher = _sut.Encrypt("Alérgica al pollo");

        cipher.ShouldNotContain("pollo");
        cipher.ShouldStartWith(AesGcmFieldEncryptor.Prefix);
    }

    [Fact]
    public void Encrypt_SameTextTwice_ProducesDifferentCiphers()
    {
        _sut.Encrypt("Luna").ShouldNotBe(_sut.Encrypt("Luna"));
    }

    [Fact]
    public void Decrypt_TamperedCipher_Throws()
    {
        string cipher = _sut.Encrypt("Alérgica al pollo");
        byte[] bytes = Convert.FromBase64String(cipher[AesGcmFieldEncryptor.Prefix.Length..]);
        bytes[^1] ^= 0xFF;
        string tampered = AesGcmFieldEncryptor.Prefix + Convert.ToBase64String(bytes);

        Should.Throw<CryptographicException>(() => _sut.Decrypt(tampered));
    }

    [Fact]
    public void Decrypt_WithAnotherKey_Throws()
    {
        string cipher = _sut.Encrypt("Alérgica al pollo");
        AesGcmFieldEncryptor other = new(new EncryptionOptions
        {
            Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
        });

        Should.Throw<CryptographicException>(() => other.Decrypt(cipher));
    }

    [Theory]
    [InlineData("")]
    [InlineData("bm90LTMyLWJ5dGVz")] // "not-32-bytes"
    [InlineData("%%% not base64 %%%")]
    public void Constructor_KeyThatIsNot32BytesOfBase64_Throws(string key) =>
        Should.Throw<InvalidOperationException>(() => new AesGcmFieldEncryptor(new EncryptionOptions { Key = key }));
}
