using Waggo.Application.Common.Interfaces;

namespace Waggo.Application.Walks.Commands.CancelWalk;

public sealed record CancelWalkCommand(Guid WalkId) : ICommand<WalkResponse>;
