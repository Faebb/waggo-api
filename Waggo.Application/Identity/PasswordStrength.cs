using System.Text.RegularExpressions;

namespace Waggo.Application.Identity;

/// <summary>The password rule of RNF-001, shared by every flow that sets a password.</summary>
internal static partial class PasswordStrength
{
    public const int MinLength = 8;

    /// <summary>At least <see cref="MinLength"/> characters, an uppercase letter and a digit.</summary>
    public static bool IsStrong(string? password) =>
        password is not null && password.Length >= MinLength && Requirements().IsMatch(password);

    [GeneratedRegex(@"(?=.*[A-Z])(?=.*\d)")]
    private static partial Regex Requirements();
}
