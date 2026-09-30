using Waggo.Api.Infrastructure.Responses;
using Waggo.Application.Common.Extensions;
using Waggo.Application.Common.Interfaces;
using Waggo.Application.Notifications;
using Waggo.Application.Notifications.Commands.MarkNotificationsRead;
using Waggo.Application.Notifications.Queries.ListMyNotifications;
using Waggo.Domain.Common;

namespace Waggo.Api.Endpoints.Notifications;

/// <summary>RF-014: each user's inbox of walk notifications, for owners and walkers alike.</summary>
internal static class NotificationsEndpoints
{
    public static IEndpointRouteBuilder MapNotificationsEndpoints(this IEndpointRouteBuilder routes)
    {
        RouteGroupBuilder group = routes.MapGroup("/notifications")
            .WithTags("Notifications")
            .RequireAuthorization();

        group.MapGet("/", ListMineAsync)
            .WithName("ListMyNotifications")
            .Produces<WaggoApiResponse<NotificationsResponse>>();

        group.MapPost("/read", MarkReadAsync)
            .WithName("MarkNotificationsRead")
            .Produces<WaggoApiResponse<NotificationsResponse>>();

        return routes;
    }

    private static async Task<IResult> ListMineAsync(
        IQueryHandler<ListMyNotificationsQuery, NotificationsResponse> handler,
        ILogger<ListMyNotificationsQuery> logger,
        CancellationToken cancellationToken)
    {
        WaggoResponse<NotificationsResponse> response =
            await handler.HandleAsync(new ListMyNotificationsQuery(), cancellationToken);
        response.WriteLogs(logger, "ListMyNotifications");
        return response.ToApiResult();
    }

    private static async Task<IResult> MarkReadAsync(
        ICommandHandler<MarkNotificationsReadCommand, NotificationsResponse> handler,
        ILogger<MarkNotificationsReadCommand> logger,
        CancellationToken cancellationToken)
    {
        WaggoResponse<NotificationsResponse> response =
            await handler.HandleAsync(new MarkNotificationsReadCommand(), cancellationToken);
        response.WriteLogs(logger, "MarkNotificationsRead");
        return response.ToApiResult();
    }
}
