namespace Waggo.Infrastructure.Options.Identity;

/// <summary>Amazon SES settings of <see cref="EmailOptions"/> ("Email:Ses").</summary>
public sealed class SesOptions
{
    /// <summary>AWS region of the verified identity, for example <c>us-east-1</c>. Identities are per region.</summary>
    public string Region { get; set; } = string.Empty;

    /// <summary>Optional SES configuration set (bounce and complaint events).</summary>
    public string? ConfigurationSet { get; set; }

    /// <summary>How long to wait for SES before giving up, so a registration never hangs on the mail provider.</summary>
    public TimeSpan SendTimeout { get; set; } = TimeSpan.FromSeconds(10);
}
