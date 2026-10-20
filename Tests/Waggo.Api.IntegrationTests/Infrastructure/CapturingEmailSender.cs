using System.Collections.Concurrent;
using Waggo.Application.Common.Interfaces.Identity;

namespace Waggo.Api.IntegrationTests.Infrastructure;

/// <summary>Test double for <see cref="IEmailSender"/>: keeps the last email sent to each address in memory.</summary>
public sealed class CapturingEmailSender : IEmailSender
{
    private readonly ConcurrentDictionary<string, string> _lastBodyByRecipient = new();

    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        _lastBodyByRecipient[to] = body;
        return Task.CompletedTask;
    }

    public string LastBodySentTo(string recipient) => _lastBodyByRecipient[recipient];
}
