using Waggo.Domain.Common;

namespace Waggo.Application.Abstractions;

/// <summary>Marker for use cases that change state.</summary>
#pragma warning disable CA1040 // Marker interface is intentional
public interface ICommand<TResponse>;
#pragma warning restore CA1040

public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<WaggoResponse<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken);
}
