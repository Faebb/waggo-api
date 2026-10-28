namespace Waggo.Domain.Enums.Identity;

/// <summary>Accounts we accept instead of a password (ADR-017). Apple is out of scope for now.</summary>
public enum ExternalProvider
{
    Google = 1,
    Microsoft = 2,
}
