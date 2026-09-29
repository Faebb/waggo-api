using Waggo.Domain.Common;

namespace Waggo.Application.Common.Interfaces;

public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<WaggoResponse<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
