using Waggo.Domain.Common;
using Waggo.Domain.Enums.Walks;
using Waggo.Domain.Errors.Messaging;

namespace Waggo.Domain.Entities.Messaging;

/// <summary>A chat message between the owner and the walker of a walk (RF-013).</summary>
public sealed class WalkMessage
{
    public const int MaxTextLength = 1000;

    // Used by EF Core to materialize the entity.
    private WalkMessage()
    {
    }

    public Guid Id { get; private set; }

    public Guid WalkId { get; private set; }

    public WalkParty SentBy { get; private set; }

    public string Text { get; private set; } = string.Empty;

    public DateTimeOffset SentAt { get; private set; }

    public static WaggoResponse<WalkMessage> Send(Guid walkId, WalkParty sentBy, string text, DateTimeOffset now)
    {
        WaggoResponse<WalkMessage> response = new();
        string trimmed = text?.Trim() ?? string.Empty;

        if (trimmed.Length is 0 or > MaxTextLength)
        {
            response.AddError(MessageErrors.InvalidText);
            return response;
        }

        response.Data = new WalkMessage
        {
            Id = Guid.CreateVersion7(now),
            WalkId = walkId,
            SentBy = sentBy,
            Text = trimmed,
            SentAt = now,
        };
        return response;
    }
}
