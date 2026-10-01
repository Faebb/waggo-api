using Waggo.Application.Common.Interfaces;

namespace Waggo.Application.Notifications.Commands.MarkNotificationsRead;

public sealed record MarkNotificationsReadCommand : ICommand<NotificationsResponse>;
