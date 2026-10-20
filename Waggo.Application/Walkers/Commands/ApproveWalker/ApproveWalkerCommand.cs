using Waggo.Application.Common.Interfaces;

namespace Waggo.Application.Walkers.Commands.ApproveWalker;

public sealed record ApproveWalkerCommand(Guid ProfileId) : ICommand<WalkerProfileResponse>;
