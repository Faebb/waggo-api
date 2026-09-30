using Waggo.Domain.Common;
using Waggo.Domain.Enums.Pricing;
using Waggo.Domain.Enums.Walks;
using Waggo.Domain.Errors.Walks;
using Waggo.Domain.ValueObjects.Pricing;
using Waggo.Domain.ValueObjects.Walks;

namespace Waggo.Domain.Entities.Walks;

/// <summary>
/// A walk requested by an owner (RF-007). Like a ride in Uber: the fare is frozen when it is requested, and the walk
/// moves through its states (<see cref="WalkStatus"/>) only through the methods of this class.
/// </summary>
public sealed class Walk
{
    public const int MaxPets = 3;
    public const int MinAddressLength = 5;
    public const int MaxAddressLength = 200;
    public const int MaxNotesLength = 500;
    public const int MaxDaysAhead = 14;

    // A request "for now" may reach the server a few seconds late: that small drift is not "in the past".
    private static readonly TimeSpan s_clockTolerance = TimeSpan.FromMinutes(1);

    private List<Guid> _petIds = [];

    // Used by EF Core to materialize the entity.
    private Walk()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>Id of the owner in the identity provider (<c>sub</c> claim).</summary>
    public string OwnerId { get; private set; } = string.Empty;

    public IReadOnlyList<Guid> PetIds => _petIds;

    public WalkType WalkType { get; private set; }

    public int DurationMinutes { get; private set; }

    public string PickupAddress { get; private set; } = string.Empty;

    public GeoPoint PickupLocation { get; private set; } = null!;

    public DateTimeOffset ScheduledFor { get; private set; }

    public string? Notes { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public decimal Total { get; private set; }

    public decimal Commission { get; private set; }

    public decimal WalkerPayout { get; private set; }

    public WalkStatus Status { get; private set; }

    /// <summary>Walker that took the walk; null while it is requested.</summary>
    public string? WalkerId { get; private set; }

    public DateTimeOffset RequestedAt { get; private set; }

    public DateTimeOffset? CancelledAt { get; private set; }

    /// <summary>
    /// Creates a requested walk, reporting every broken rule at once. <paramref name="scheduledFor"/> null means now.
    /// </summary>
    public static WaggoResponse<Walk> Request(
        string ownerId,
        IReadOnlyList<Guid> petIds,
        WalkType walkType,
        WalkDuration duration,
        string pickupAddress,
        GeoPoint pickupLocation,
        DateTimeOffset? scheduledFor,
        string? notes,
        FareBreakdown fare,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        ArgumentNullException.ThrowIfNull(petIds);
        ArgumentNullException.ThrowIfNull(duration);
        ArgumentNullException.ThrowIfNull(pickupLocation);
        ArgumentNullException.ThrowIfNull(fare);
        WaggoResponse<Walk> response = new();

        string address = pickupAddress?.Trim() ?? string.Empty;
        string? trimmedNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        DateTimeOffset when = scheduledFor ?? now;

        if (petIds.Count is 0 or > MaxPets || petIds.Distinct().Count() != petIds.Count)
        {
            response.AddError(WalkErrors.InvalidPets);
        }

        if (address.Length is < MinAddressLength or > MaxAddressLength)
        {
            response.AddError(WalkErrors.InvalidAddress);
        }

        if (when < now - s_clockTolerance || when > now.AddDays(MaxDaysAhead))
        {
            response.AddError(WalkErrors.InvalidSchedule);
        }

        if (trimmedNotes?.Length > MaxNotesLength)
        {
            response.AddError(WalkErrors.InvalidNotes);
        }

        if (!response.IsValid)
        {
            return response;
        }

        response.Data = new Walk
        {
            Id = Guid.CreateVersion7(now),
            OwnerId = ownerId,
            _petIds = [.. petIds],
            WalkType = walkType,
            DurationMinutes = duration.Minutes,
            PickupAddress = address,
            PickupLocation = pickupLocation,
            ScheduledFor = when,
            Notes = trimmedNotes,
            Currency = fare.Total.Currency,
            Total = fare.Total.Amount,
            Commission = fare.Commission.Amount,
            WalkerPayout = fare.WalkerPayout.Amount,
            Status = WalkStatus.Requested,
            RequestedAt = now,
        };
        return response;
    }

    /// <summary>The owner cancels. Only possible before the walk starts.</summary>
    public WaggoResponse<Walk> Cancel(DateTimeOffset now)
    {
        WaggoResponse<Walk> response = new();

        if (Status is not (WalkStatus.Requested or WalkStatus.Accepted))
        {
            response.AddError(WalkErrors.CannotCancel);
            return response;
        }

        Status = WalkStatus.Cancelled;
        CancelledAt = now;
        response.Data = this;
        return response;
    }
}
