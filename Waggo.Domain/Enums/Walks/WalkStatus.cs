namespace Waggo.Domain.Enums.Walks;

/// <summary>Life cycle of a walk (RF-007): requested → accepted → in progress → completed, or cancelled.</summary>
public enum WalkStatus
{
    /// <summary>The owner asked for it; looking for a walker.</summary>
    Requested = 1,

    /// <summary>A walker took it and will pick the dogs up.</summary>
    Accepted = 2,

    InProgress = 3,
    Completed = 4,
    Cancelled = 5,
}
