using Amazon.Runtime;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using NSubstitute.ExceptionExtensions;
using Waggo.Domain.Exceptions;
using Waggo.Infrastructure.Options.Identity;
using Waggo.Infrastructure.Services.Identity;

namespace Waggo.Infrastructure.UnitTests.Services.Identity;

/// <summary>RF-001 (ADR-016): the confirmation email goes out through Amazon SES.</summary>
public class SesEmailSenderTests
{
    private readonly IAmazonSimpleEmailServiceV2 _ses = Substitute.For<IAmazonSimpleEmailServiceV2>();

    private SesEmailSender Sut(Action<EmailOptions>? configure = null)
    {
        EmailOptions options = new()
        {
            Provider = EmailProvider.Ses,
            FromAddress = "no-reply@waggo.app",
            FromName = "Waggo",
            Ses = new SesOptions { Region = "us-east-1" },
        };
        configure?.Invoke(options);
        return new SesEmailSender(_ses, Microsoft.Extensions.Options.Options.Create(options));
    }

    private static async Task<SendEmailResponse> NeverAnswersAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.Infinite, cancellationToken);
        return new SendEmailResponse();
    }

    [Fact]
    public async Task SendAsync_ValidEmail_SendsItThroughSesWithSenderRecipientSubjectAndBody()
    {
        SendEmailRequest? sent = null;
        await _ses.SendEmailAsync(Arg.Do<SendEmailRequest>(request => sent = request), Arg.Any<CancellationToken>());

        await Sut().SendAsync("ana@mail.com", "Tu código", "Hola 123456", CancellationToken.None);

        sent.ShouldNotBeNull();
        sent.FromEmailAddress.ShouldBe("Waggo <no-reply@waggo.app>");
        sent.Destination.ToAddresses.ShouldBe(["ana@mail.com"]);
        sent.Content.Simple.Subject.Data.ShouldBe("Tu código");
        sent.Content.Simple.Body.Text.Data.ShouldBe("Hola 123456");
        sent.ConfigurationSetName.ShouldBeNull();
    }

    [Fact]
    public async Task SendAsync_NoDisplayName_UsesOnlyTheAddressAsSender()
    {
        SendEmailRequest? sent = null;
        await _ses.SendEmailAsync(Arg.Do<SendEmailRequest>(request => sent = request), Arg.Any<CancellationToken>());

        await Sut(options => options.FromName = string.Empty)
            .SendAsync("ana@mail.com", "Asunto", "Cuerpo", CancellationToken.None);

        sent.ShouldNotBeNull();
        sent.FromEmailAddress.ShouldBe("no-reply@waggo.app");
    }

    [Fact]
    public async Task SendAsync_ConfigurationSetConfigured_SendsItWithTheRequest()
    {
        SendEmailRequest? sent = null;
        await _ses.SendEmailAsync(Arg.Do<SendEmailRequest>(request => sent = request), Arg.Any<CancellationToken>());

        await Sut(options => options.Ses.ConfigurationSet = "waggo-transactional")
            .SendAsync("ana@mail.com", "Asunto", "Cuerpo", CancellationToken.None);

        sent.ShouldNotBeNull();
        sent.ConfigurationSetName.ShouldBe("waggo-transactional");
    }

    [Fact]
    public async Task SendAsync_SesRejectsTheMessage_ThrowsSendFailedKeepingTheAwsReasonOutOfTheUserMessage()
    {
        _ses.SendEmailAsync(Arg.Any<SendEmailRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new MessageRejectedException("Email address is not verified."));

        ExternalServiceException exception = await Should.ThrowAsync<ExternalServiceException>(
            () => Sut().SendAsync("ana@mail.com", "Asunto", "Cuerpo", CancellationToken.None));

        exception.Error.Code.ShouldBe("Email.SendFailed");
        exception.Error.Message.ShouldNotContain("verified");
        exception.Message.ShouldContain("Email address is not verified.");
    }

    [Fact]
    public async Task SendAsync_NoCredentialsOrNoNetwork_ThrowsSendFailed()
    {
        _ses.SendEmailAsync(Arg.Any<SendEmailRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new AmazonClientException("Unable to get IAM security credentials."));

        ExternalServiceException exception = await Should.ThrowAsync<ExternalServiceException>(
            () => Sut().SendAsync("ana@mail.com", "Asunto", "Cuerpo", CancellationToken.None));

        exception.Error.Code.ShouldBe("Email.SendFailed");
    }

    [Fact]
    public async Task SendAsync_SesDoesNotAnswerInTime_ThrowsSendFailedInsteadOfHanging()
    {
        _ses.SendEmailAsync(Arg.Any<SendEmailRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => NeverAnswersAsync(call.Arg<CancellationToken>()));

        ExternalServiceException exception = await Should.ThrowAsync<ExternalServiceException>(
            () => Sut(options => options.Ses.SendTimeout = TimeSpan.FromMilliseconds(50))
                .SendAsync("ana@mail.com", "Asunto", "Cuerpo", CancellationToken.None));

        exception.Error.Code.ShouldBe("Email.SendFailed");
    }

    [Fact]
    public async Task SendAsync_CallerCancels_PropagatesTheCancellationAndIsNotASendFailure()
    {
        _ses.SendEmailAsync(Arg.Any<SendEmailRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => NeverAnswersAsync(call.Arg<CancellationToken>()));
        using CancellationTokenSource cancellation = new();
        await cancellation.CancelAsync();

        await Should.ThrowAsync<TaskCanceledException>(
            () => Sut().SendAsync("ana@mail.com", "Asunto", "Cuerpo", cancellation.Token));
    }
}
