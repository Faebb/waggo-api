using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Messaging;
using Waggo.Application.Common.Interfaces.Walks;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Messaging;
using Waggo.Domain.Entities.Walks;
using Waggo.Domain.Errors.Walks;
using Waggo.Domain.Exceptions;

namespace Waggo.Application.Messaging.Queries.ListMessages;

/// <summary>RF-013: the owner and the walker read the chat, also after the walk ends (read only).</summary>
internal sealed class ListMessagesHandler(
    IWalkRepository walks,
    IWalkMessageRepository messages,
    ICurrentUser currentUser)
    : IQueryHandler<ListMessagesQuery, IReadOnlyList<WalkMessageResponse>>
{
    public async Task<WaggoResponse<IReadOnlyList<WalkMessageResponse>>> HandleAsync(
        ListMessagesQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        Walk? walk = await walks.GetAsync(query.WalkId, cancellationToken);
        if (walk?.PartyOf(currentUser.Id) is null)
        {
            throw new NotFoundException(WalkErrors.NotFound, $"Walk {query.WalkId} not found for {currentUser.Id}");
        }

        List<WalkMessage> ordered =
            [.. (await messages.ListByWalkAsync(walk.Id, cancellationToken)).OrderBy(m => m.SentAt).ThenBy(m => m.Id)];

        // The phone asks only for what arrived after the last message it has.
        int from = query.After is { } after ? ordered.FindIndex(message => message.Id == after) + 1 : 0;
        return new WaggoResponse<IReadOnlyList<WalkMessageResponse>>
        {
            Data = [.. ordered.Skip(from).Select(WalkMessageResponse.From)],
        };
    }
}
