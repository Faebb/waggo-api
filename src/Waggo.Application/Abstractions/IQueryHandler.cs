using Waggo.Domain.Common;

namespace Waggo.Application.Abstractions;

/// <summary>Marker for read-only use cases.</summary>
#pragma warning disable CA1040 // Marker interface is intentional
public interface IQuery<TResponse>;
#pragma warning restore CA1040

public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<Result<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
