using Microsoft.Extensions.Options;

namespace Waggo.Infrastructure.Options.Identity;

/// <summary>Fails the startup when SES is chosen but the settings it needs are missing.</summary>
public sealed class EmailOptionsValidator : IValidateOptions<EmailOptions>
{
    public ValidateOptionsResult Validate(string? name, EmailOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.Provider != EmailProvider.Ses)
        {
            return ValidateOptionsResult.Success;
        }

        List<string> failures = [];
        if (string.IsNullOrWhiteSpace(options.FromAddress))
        {
            failures.Add("Email:FromAddress is not configured (for example no-reply@yourdomain).");
        }

        if (string.IsNullOrWhiteSpace(options.Ses.Region))
        {
            failures.Add("Email:Ses:Region is not configured (for example us-east-1).");
        }

        if (options.Ses.SendTimeout <= TimeSpan.Zero)
        {
            failures.Add("Email:Ses:SendTimeout must be greater than zero.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
