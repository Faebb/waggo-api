using Waggo.Domain.Entities.Walks;
using Waggo.Domain.Enums.Notifications;
using Waggo.Domain.Enums.Walks;

namespace Waggo.Domain.Entities.Notifications;

/// <summary>
/// A notice for one user about a moment of a walk (RF-014). Shown in the app's inbox; the future push sender will
/// deliver the ones not sent yet (the table works as an outbox, RNF-008).
/// </summary>
public sealed class Notification
{
    private static readonly Dictionary<NotificationKind, (string Title, string Body, NotificationPriority Priority)>
        s_texts = new()
        {
            [NotificationKind.WalkAccepted] = (
                "Tu paseo fue aceptado",
                "Un paseador verificado va a recoger a tu perro.",
                NotificationPriority.Normal),
            [NotificationKind.WalkStarted] = (
                "Tu perro salió a pasear",
                "Sigue el recorrido en vivo desde la app.",
                NotificationPriority.Normal),
            [NotificationKind.WalkFinished] = (
                "El paseo terminó",
                "Tu perro está de vuelta. Revisa el resumen del recorrido.",
                NotificationPriority.Normal),
            [NotificationKind.WalkPaid] = (
                "Te pagamos el paseo",
                "Tu parte ya está en tus ganancias.",
                NotificationPriority.Normal),
            [NotificationKind.WalkCancelled] = (
                "El dueño canceló el paseo",
                "Ya no tienes que recoger al perro.",
                NotificationPriority.Normal),
            [NotificationKind.Emergency] = (
                "Emergencia en el paseo",
                "Abre el paseo para ver la alerta y la ubicación.",
                NotificationPriority.High),
            [NotificationKind.Geofence] = (
                "El paseo salió de la zona",
                "El paseador se alejó más de lo previsto del punto de recogida.",
                NotificationPriority.High),
            [NotificationKind.Anomaly] = (
                "El paseador lleva un rato detenido",
                "Lleva varios minutos en el mismo lugar. Abre el paseo para revisar.",
                NotificationPriority.High),
        };

    // Used by EF Core to materialize the entity.
    private Notification()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>Who receives it (<c>sub</c> claim).</summary>
    public string UserId { get; private set; } = string.Empty;

    public Guid WalkId { get; private set; }

    /// <summary>The side of the walk the user is on, so the app opens the right screen.</summary>
    public WalkParty RecipientParty { get; private set; }

    public NotificationKind Kind { get; private set; }

    public NotificationPriority Priority { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Body { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ReadAt { get; private set; }

    /// <summary>A notice of <paramref name="kind"/> for the <paramref name="recipient"/> side of the walk.</summary>
    public static Notification ForWalk(Walk walk, WalkParty recipient, NotificationKind kind, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(walk);
        string userId = recipient == WalkParty.Owner
            ? walk.OwnerId
            : walk.WalkerId ?? throw new InvalidOperationException($"Walk {walk.Id} has no walker to notify.");
        (string title, string body, NotificationPriority priority) = s_texts[kind];

        return new Notification
        {
            Id = Guid.CreateVersion7(now),
            UserId = userId,
            WalkId = walk.Id,
            RecipientParty = recipient,
            Kind = kind,
            Priority = priority,
            Title = title,
            Body = body,
            CreatedAt = now,
        };
    }

    public void MarkRead(DateTimeOffset now) => ReadAt ??= now;
}
