using System.Collections.Concurrent;
using Waggo.Application.Common.Interfaces.Identity;

namespace Waggo.Api.IntegrationTests.Infrastructure;

/// <summary>Test double for <see cref="IEmailSender"/>: keeps the last email sent to each address in memory.</summary>
public sealed class CapturingEmailSender : IEmailSender
{
    private readonly ConcurrentDictionary<string, string> _lastBodyByRecipient = new();

    /// <summary>When set, every send throws it, like a mail provider that is down. Reset it when the test ends.</summary>
    public Exception? FailWith { get; set; }

    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        if (FailWith is not null)
        {
            return Task.FromException(FailWith);
        }

        _lastBodyByRecipient[to] = body;
        return Task.CompletedTask;
    }

    public string LastBodySentTo(string recipient) => _lastBodyByRecipient[recipient];
}
