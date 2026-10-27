using Microsoft.Extensions.Options;
using Waggo.Infrastructure.Options.Identity;

namespace Waggo.Infrastructure.UnitTests.Options.Identity;

/// <summary>The API must not start with a half-configured SES provider.</summary>
public class EmailOptionsValidatorTests
{
    private static ValidateOptionsResult Validate(EmailOptions options) =>
        new EmailOptionsValidator().Validate(name: null, options);

    [Fact]
    public void Validate_DevelopmentProviderWithNothingElse_Succeeds()
    {
        Validate(new EmailOptions { Provider = EmailProvider.Development }).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_SesWithSenderAndRegion_Succeeds()
    {
        EmailOptions options = new()
        {
            Provider = EmailProvider.Ses,
            FromAddress = "no-reply@waggo.app",
            Ses = new SesOptions { Region = "us-east-1" },
        };

        Validate(options).Succeeded.ShouldBeTrue();
    }

    [Fact]
    public void Validate_SesWithoutSender_FailsSayingWhatIsMissing()
    {
        EmailOptions options = new() { Provider = EmailProvider.Ses, Ses = new SesOptions { Region = "us-east-1" } };

        ValidateOptionsResult result = Validate(options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("Email:FromAddress");
    }

    [Fact]
    public void Validate_SesWithoutRegion_FailsSayingWhatIsMissing()
    {
        EmailOptions options = new() { Provider = EmailProvider.Ses, FromAddress = "no-reply@waggo.app" };

        ValidateOptionsResult result = Validate(options);

        result.Failed.ShouldBeTrue();
        result.FailureMessage.ShouldContain("Email:Ses:Region");
    }

    [Fact]
    public void Validate_SesWithNonPositiveTimeout_Fails()
    {
        EmailOptions options = new()
        {
            Provider = EmailProvider.Ses,
            FromAddress = "no-reply@waggo.app",
            Ses = new SesOptions { Region = "us-east-1", SendTimeout = TimeSpan.Zero },
        };

        Validate(options).FailureMessage.ShouldNotBeNull().ShouldContain("Email:Ses:SendTimeout");
    }
}
