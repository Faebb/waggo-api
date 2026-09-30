using Waggo.Domain.Entities.Messaging;

namespace Waggo.Application.Messaging;

public sealed record WalkMessageResponse(Guid Id, string SentBy, string Text, DateTimeOffset SentAt)
{
    public static WalkMessageResponse From(WalkMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return new WalkMessageResponse(message.Id, message.SentBy.ToString(), message.Text, message.SentAt);
    }
}
