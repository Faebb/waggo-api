using Waggo.Application.Walks.Commands.RequestWalk;
using Waggo.Domain.Enums.Pricing;

namespace Waggo.Api.Endpoints.Walks.Requests;

/// <summary>Body of <c>POST /api/v1/walks</c>. Raw values: they are validated before use.</summary>
public sealed record RequestWalkRequest(
    List<Guid>? PetIds,
    string? WalkType,
    int? DurationMinutes,
    string? PickupAddress,
    double? Latitude,
    double? Longitude,
    DateTimeOffset? ScheduledFor,
    string? Notes)
{
    /// <summary>Only call it after the request passed <c>RequestWalkRequestValidator</c>.</summary>
    public RequestWalkCommand ToCommand() =>
        new(
            PetIds!,
            Enum.Parse<WalkType>(WalkType!, ignoreCase: true),
            DurationMinutes!.Value,
            PickupAddress!,
            Latitude!.Value,
            Longitude!.Value,
            ScheduledFor,
            Notes);
}
