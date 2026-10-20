using Waggo.Application.Common.Interfaces;

namespace Waggo.Application.Messaging.Queries.ListMessages;

/// <summary>
/// The chat of a walk in the order it was written; with <paramref name="After"/>, only the newer ones.
/// </summary>
public sealed record ListMessagesQuery(Guid WalkId, Guid? After) : IQuery<IReadOnlyList<WalkMessageResponse>>;
