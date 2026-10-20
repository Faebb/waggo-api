using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Waggo.Application.Common.Interfaces;
using Waggo.Application.Messaging;
using Waggo.Application.Messaging.Commands.SendMessage;
using Waggo.Application.Messaging.Queries.ListMessages;
using Waggo.Application.Notifications;
using Waggo.Application.Notifications.Commands.MarkNotificationsRead;
using Waggo.Application.Notifications.Queries.ListMyNotifications;
using Waggo.Application.Payments;
using Waggo.Application.Payments.Queries.GetMyEarnings;
using Waggo.Application.Payments.Queries.GetWalkPayment;
using Waggo.Application.Pets;
using Waggo.Application.Pets.Commands.RegisterPet;
using Waggo.Application.Pets.Queries.GetPet;
using Waggo.Application.Pets.Queries.ListMyPets;
using Waggo.Application.Pricing.Queries.QuoteFare;
using Waggo.Application.Tracking;
using Waggo.Application.Tracking.Commands.RaiseEmergency;
using Waggo.Application.Tracking.Commands.RecordTrack;
using Waggo.Application.Tracking.Queries.GetRoute;
using Waggo.Application.Tracking.Queries.ListWalkAlerts;
using Waggo.Application.Walkers;
using Waggo.Application.Walkers.Commands.ApproveWalker;
using Waggo.Application.Walkers.Commands.RegisterWalker;
using Waggo.Application.Walkers.Commands.RejectWalker;
using Waggo.Application.Walkers.Queries.GetMyWalkerProfile;
using Waggo.Application.Walkers.Queries.ListWalkersForReview;
using Waggo.Application.Walks;
using Waggo.Application.Walks.Commands.AcceptWalk;
using Waggo.Application.Walks.Commands.CancelWalk;
using Waggo.Application.Walks.Commands.FinishWalk;
using Waggo.Application.Walks.Commands.RequestWalk;
using Waggo.Application.Walks.Commands.StartWalk;
using Waggo.Application.Walks.Queries.GetWalk;
using Waggo.Application.Walks.Queries.ListAssignedWalks;
using Waggo.Application.Walks.Queries.ListAvailableWalks;
using Waggo.Application.Walks.Queries.ListMyWalks;

namespace Waggo.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Registers every AbstractValidator<T> of this assembly (validators are internal sealed).
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly, includeInternalTypes: true);

        services.AddSingleton(TimeProvider.System);

        services.AddScoped<IQueryHandler<QuoteFareQuery, FareQuoteResponse>, QuoteFareHandler>();

        services.AddScoped<ICommandHandler<RegisterPetCommand, PetResponse>, RegisterPetHandler>();
        services.AddScoped<IQueryHandler<ListMyPetsQuery, IReadOnlyList<PetResponse>>, ListMyPetsHandler>();
        services.AddScoped<IQueryHandler<GetPetQuery, PetResponse>, GetPetHandler>();

        services.AddScoped<ICommandHandler<RequestWalkCommand, WalkResponse>, RequestWalkHandler>();
        services.AddScoped<ICommandHandler<CancelWalkCommand, WalkResponse>, CancelWalkHandler>();
        services.AddScoped<IQueryHandler<ListMyWalksQuery, IReadOnlyList<WalkResponse>>, ListMyWalksHandler>();
        services.AddScoped<IQueryHandler<GetWalkQuery, WalkResponse>, GetWalkHandler>();
        services.AddScoped<ICommandHandler<AcceptWalkCommand, WalkResponse>, AcceptWalkHandler>();
        services.AddScoped<IQueryHandler<ListAvailableWalksQuery, IReadOnlyList<AvailableWalkResponse>>,
            ListAvailableWalksHandler>();
        services.AddScoped<IQueryHandler<ListAssignedWalksQuery, IReadOnlyList<WalkResponse>>,
            ListAssignedWalksHandler>();
        services.AddScoped<ICommandHandler<StartWalkCommand, WalkResponse>, StartWalkHandler>();
        services.AddScoped<ICommandHandler<FinishWalkCommand, WalkResponse>, FinishWalkHandler>();

        services.AddScoped<ICommandHandler<RecordTrackCommand, int>, RecordTrackHandler>();
        services.AddScoped<IQueryHandler<GetRouteQuery, RouteResponse>, GetRouteHandler>();
        services.AddScoped<ICommandHandler<RaiseEmergencyCommand, WalkAlertResponse>, RaiseEmergencyHandler>();
        services.AddScoped<IQueryHandler<ListWalkAlertsQuery, IReadOnlyList<WalkAlertResponse>>,
            ListWalkAlertsHandler>();
        services.AddScoped<ICommandHandler<SendMessageCommand, WalkMessageResponse>, SendMessageHandler>();
        services.AddScoped<IQueryHandler<ListMessagesQuery, IReadOnlyList<WalkMessageResponse>>,
            ListMessagesHandler>();
        services.AddScoped<ICommandHandler<RegisterWalkerCommand, WalkerProfileResponse>, RegisterWalkerHandler>();
        services.AddScoped<IQueryHandler<GetMyWalkerProfileQuery, WalkerProfileResponse>, GetMyWalkerProfileHandler>();
        services.AddScoped<IQueryHandler<ListWalkersForReviewQuery, IReadOnlyList<WalkerProfileResponse>>,
            ListWalkersForReviewHandler>();
        services.AddScoped<ICommandHandler<ApproveWalkerCommand, WalkerProfileResponse>, ApproveWalkerHandler>();
        services.AddScoped<ICommandHandler<RejectWalkerCommand, WalkerProfileResponse>, RejectWalkerHandler>();
        services.AddScoped<IQueryHandler<GetWalkPaymentQuery, WalkPaymentResponse>, GetWalkPaymentHandler>();
        services.AddScoped<IQueryHandler<GetMyEarningsQuery, WalkerEarningsResponse>, GetMyEarningsHandler>();
        services.AddScoped<IQueryHandler<ListMyNotificationsQuery, NotificationsResponse>,
            ListMyNotificationsHandler>();
        services.AddScoped<ICommandHandler<MarkNotificationsReadCommand, NotificationsResponse>,
            MarkNotificationsReadHandler>();
        return services;
    }
}
