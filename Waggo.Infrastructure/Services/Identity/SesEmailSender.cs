using Amazon.Runtime;
using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using Microsoft.Extensions.Options;
using Waggo.Application.Common.Interfaces.Identity;
using Waggo.Domain.Errors.Identity;
using Waggo.Domain.Exceptions;
using Waggo.Infrastructure.Options.Identity;

namespace Waggo.Infrastructure.Services.Identity;

/// <summary>
/// Sends the API's transactional email through Amazon SES (RF-001, ADR-016). A failure of the provider never reaches
/// the user as-is: it becomes <c>Email.SendFailed</c> (503) and the AWS reason stays in the exception for the log.
/// </summary>
public sealed class SesEmailSender(IAmazonSimpleEmailServiceV2 ses, IOptions<EmailOptions> options) : IEmailSender
{
    private const string Utf8 = "UTF-8";

    public async Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        EmailOptions settings = options.Value;
        SendEmailRequest request = new()
        {
            FromEmailAddress = string.IsNullOrWhiteSpace(settings.FromName)
                ? settings.FromAddress
                : $"{settings.FromName} <{settings.FromAddress}>",
            Destination = new Destination { ToAddresses = [to] },
            Content = new EmailContent
            {
                Simple = new Message
                {
                    Subject = new Content { Data = subject, Charset = Utf8 },
                    Body = new Body { Text = new Content { Data = body, Charset = Utf8 } },
                },
            },
            ConfigurationSetName = settings.Ses.ConfigurationSet,
        };

        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(settings.Ses.SendTimeout);

        try
        {
            await ses.SendEmailAsync(request, timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ExternalServiceException(
                EmailErrors.SendFailed,
                $"SES did not answer within {settings.Ses.SendTimeout}.");
        }
        catch (Exception exception) when (exception is AmazonServiceException or AmazonClientException
            or HttpRequestException)
        {
            throw new ExternalServiceException(
                EmailErrors.SendFailed,
                $"SES could not send the email: {exception.Message}",
                exception);
        }
    }
}
