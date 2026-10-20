using Waggo.Application.Messaging.Commands.SendMessage;

namespace Waggo.Api.Endpoints.Messaging.Requests;

/// <summary>Body of <c>POST /api/v1/walks/{id}/messages</c>. An empty text is a business error.</summary>
public sealed record SendMessageRequest(string? Text)
{
    public SendMessageCommand ToCommand(Guid walkId) => new(walkId, Text ?? string.Empty);
}
