using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Messaging;
using Waggo.Application.Common.Interfaces.Walks;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Messaging;
using Waggo.Domain.Entities.Walks;
using Waggo.Domain.Enums.Walks;
using Waggo.Domain.Errors.Messaging;
using Waggo.Domain.Errors.Walks;
using Waggo.Domain.Exceptions;

namespace Waggo.Application.Messaging.Commands.SendMessage;

/// <summary>RF-013: the owner or the walker writes to the other while the walk is accepted or going on.</summary>
internal sealed class SendMessageHandler(
    IWalkRepository walks,
    IWalkMessageRepository messages,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : ICommandHandler<SendMessageCommand, WalkMessageResponse>
{
    public async Task<WaggoResponse<WalkMessageResponse>> HandleAsync(
        SendMessageCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        WaggoResponse<WalkMessageResponse> response = new();

        Walk? walk = await walks.GetAsync(command.WalkId, cancellationToken);
        WalkParty? party = walk?.PartyOf(currentUser.Id);
        if (walk is null || party is null)
        {
            throw new NotFoundException(WalkErrors.NotFound, $"Walk {command.WalkId} not found for {currentUser.Id}");
        }

        if (walk.Status is not (WalkStatus.Accepted or WalkStatus.InProgress))
        {
            response.AddError(MessageErrors.ChatClosed);
            return response;
        }

        WaggoResponse<WalkMessage> message =
            WalkMessage.Send(walk.Id, party.Value, command.Text, timeProvider.GetUtcNow());
        response.ConcatStacks(message);
        if (!response.IsValid)
        {
            return response;
        }

        await messages.AddAsync(message.Data, cancellationToken);
        response.Data = WalkMessageResponse.From(message.Data);
        return response;
    }
}
