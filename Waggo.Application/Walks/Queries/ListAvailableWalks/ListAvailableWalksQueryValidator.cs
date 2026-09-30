using FluentValidation;
using Waggo.Application.Common.Extensions;
using Waggo.Domain.Errors.Walks;

namespace Waggo.Application.Walks.Queries.ListAvailableWalks;

/// <summary>The position is optional, but when it comes it needs both coordinates and valid ranges.</summary>
internal sealed class ListAvailableWalksQueryValidator : AbstractValidator<ListAvailableWalksQuery>
{
    public ListAvailableWalksQueryValidator()
    {
        RuleFor(query => query)
            .Must(query => (query.Latitude, query.Longitude) switch
            {
                (null, null) => true,
                ({ } latitude, { } longitude) => latitude is >= -90 and <= 90 && longitude is >= -180 and <= 180,
                _ => false,
            })
            .WithError(WalkErrors.InvalidLocation);
    }
}
