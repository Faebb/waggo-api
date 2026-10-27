namespace Waggo.Infrastructure.Options.Identity;

/// <summary>One test account of <see cref="SeedUsersOptions"/>. Without email or password it is skipped.</summary>
public sealed class SeedUserOptions
{
    public string? Email { get; set; }

    public string? Password { get; set; }

    public string? FullName { get; set; }

    public string? Phone { get; set; }

    /// <summary>Only for the walker: the CC number of its approved profile.</summary>
    public string? DocumentNumber { get; set; }
}
