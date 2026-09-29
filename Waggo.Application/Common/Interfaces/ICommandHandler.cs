using Waggo.Domain.Common;

namespace Waggo.Application.Common.Interfaces;

public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<WaggoResponse<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
