namespace Waggo.Infrastructure.Options.Identity;

/// <summary>Who delivers the API's transactional email (<see cref="EmailOptions.Provider"/>).</summary>
public enum EmailProvider
{
    /// <summary>Writes the email to the log. Development and Testing only.</summary>
    Development = 0,

    /// <summary>Amazon SES: the only provider allowed in Production.</summary>
    Ses = 1,
}
