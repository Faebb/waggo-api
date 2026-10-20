using Waggo.Application.Common.Interfaces;

namespace Waggo.Application.Walkers.Commands.RejectWalker;

public sealed record RejectWalkerCommand(Guid ProfileId, string Reason) : ICommand<WalkerProfileResponse>;
