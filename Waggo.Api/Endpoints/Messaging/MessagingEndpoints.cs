using Waggo.Api.Endpoints.Messaging.Requests;
using Waggo.Api.Infrastructure.Responses;
using Waggo.Application.Common.Extensions;
using Waggo.Application.Common.Interfaces;
using Waggo.Application.Messaging;
using Waggo.Application.Messaging.Commands.SendMessage;
using Waggo.Application.Messaging.Queries.ListMessages;
using Waggo.Domain.Common;

namespace Waggo.Api.Endpoints.Messaging;

/// <summary>RF-013: chat of a walk. Owner or assigned walker; the handlers check who the caller is.</summary>
internal static class MessagingEndpoints
{
    public static IEndpointRouteBuilder MapMessagingEndpoints(this IEndpointRouteBuilder routes)
    {
        RouteGroupBuilder chat = routes.MapGroup("/walks/{id:guid}/messages")
            .WithTags("Messaging")
            .RequireAuthorization();

        chat.MapPost("/", SendMessageAsync)
            .WithName("SendMessage")
            .Produces<WaggoApiResponse<WalkMessageResponse>>();

        chat.MapGet("/", ListMessagesAsync)
            .WithName("ListMessages")
            .Produces<WaggoApiResponse<IReadOnlyList<WalkMessageResponse>>>();

        return routes;
    }

    private static async Task<IResult> SendMessageAsync(
        Guid id,
        SendMessageRequest request,
        ICommandHandler<SendMessageCommand, WalkMessageResponse> handler,
        ILogger<SendMessageRequest> logger,
        CancellationToken cancellationToken)
    {
        WaggoResponse<WalkMessageResponse> response =
            await handler.HandleAsync(request.ToCommand(id), cancellationToken);
        response.WriteLogs(logger, "SendMessage");
        return response.ToApiResult();
    }

    private static async Task<IResult> ListMessagesAsync(
        Guid id,
        Guid? after,
        IQueryHandler<ListMessagesQuery, IReadOnlyList<WalkMessageResponse>> handler,
        ILogger<ListMessagesQuery> logger,
        CancellationToken cancellationToken)
    {
        WaggoResponse<IReadOnlyList<WalkMessageResponse>> response =
            await handler.HandleAsync(new ListMessagesQuery(id, after), cancellationToken);
        response.WriteLogs(logger, "ListMessages");
        return response.ToApiResult();
    }
}
