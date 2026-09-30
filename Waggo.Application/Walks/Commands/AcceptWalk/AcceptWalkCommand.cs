using Waggo.Application.Common.Interfaces;

namespace Waggo.Application.Walks.Commands.AcceptWalk;

public sealed record AcceptWalkCommand(Guid WalkId) : ICommand<WalkResponse>;
