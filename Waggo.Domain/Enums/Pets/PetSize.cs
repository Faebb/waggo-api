namespace Waggo.Domain.Enums.Pets;

/// <summary>Size of a dog (RF-004). Provisional ranges pending PO validation.</summary>
public enum PetSize
{
    /// <summary>Less than 10 kg.</summary>
    Small = 1,

    /// <summary>From 10 to 25 kg.</summary>
    Medium = 2,

    /// <summary>More than 25 kg.</summary>
    Large = 3,
}
