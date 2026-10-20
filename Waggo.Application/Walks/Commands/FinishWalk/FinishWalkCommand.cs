using Waggo.Application.Common.Interfaces;

namespace Waggo.Application.Walks.Commands.FinishWalk;

public sealed record FinishWalkCommand(Guid WalkId) : ICommand<WalkResponse>;
