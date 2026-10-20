using Waggo.Application.Common.Interfaces;

namespace Waggo.Application.Walks.Commands.StartWalk;

public sealed record StartWalkCommand(Guid WalkId) : ICommand<WalkResponse>;
