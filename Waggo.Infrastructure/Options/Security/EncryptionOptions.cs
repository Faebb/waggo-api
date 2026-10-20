using System.ComponentModel.DataAnnotations;

namespace Waggo.Infrastructure.Options.Security;

/// <summary>
/// Bound from the "Encryption" section (RNF-003). The key never lives in the code: in production it comes from a
/// secret store or an environment variable (<c>Encryption__Key</c>).
/// </summary>
public sealed class EncryptionOptions
{
    public const string SectionName = "Encryption";

    /// <summary>AES-256 key: 32 random bytes encoded in base64.</summary>
    [Required]
    public string Key { get; set; } = string.Empty;
}
