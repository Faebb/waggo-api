using Waggo.Application.Common.Interfaces;

namespace Waggo.Application.Messaging.Commands.SendMessage;

public sealed record SendMessageCommand(Guid WalkId, string Text) : ICommand<WalkMessageResponse>;
