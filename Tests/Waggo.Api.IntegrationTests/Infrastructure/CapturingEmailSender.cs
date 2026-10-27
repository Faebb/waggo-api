using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Waggo.Application.Common.Interfaces.Identity;

namespace Waggo.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Test double for <see cref="IEmailSender"/>: keeps the last email sent to each address in memory, and how many.
/// </summary>
public sealed partial class CapturingEmailSender : IEmailSender
{
    private readonly ConcurrentDictionary<string, string> _lastBodyByRecipient = new();
    private readonly ConcurrentDictionary<string, int> _countByRecipient = new();

    /// <summary>When set, every send throws it, like a mail provider that is down. Reset it when the test ends.</summary>
    public Exception? FailWith { get; set; }

    public Task SendAsync(string to, string subject, string body, CancellationToken cancellationToken)
    {
        if (FailWith is not null)
        {
            return Task.FromException(FailWith);
        }

        _lastBodyByRecipient[to] = body;
        _countByRecipient.AddOrUpdate(to, 1, (_, count) => count + 1);
        return Task.CompletedTask;
    }

    public string LastBodySentTo(string recipient) => _lastBodyByRecipient[recipient];

    /// <summary>The 6-digit confirmation code of the last email sent to the address (RF-001).</summary>
    public string LastConfirmationCodeSentTo(string recipient) => SixDigits().Match(LastBodySentTo(recipient)).Value;

    public int SentCountTo(string recipient) => _countByRecipient.GetValueOrDefault(recipient);

    [GeneratedRegex(@"\b\d{6}\b")]
    private static partial Regex SixDigits();
}
