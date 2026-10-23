using FluentValidation;
using Waggo.Application.Common.Extensions;
using Waggo.Application.Common.Interfaces;
using Waggo.Application.Common.Interfaces.Payments;
using Waggo.Domain.Common;
using Waggo.Domain.Entities.Payments;

namespace Waggo.Application.Payments.Commands.RegisterPaymentMethod;

/// <summary>RF-020: the current owner registers a simulated payment method. The first one is always the default.</summary>
internal sealed class RegisterPaymentMethodHandler(
    IValidator<RegisterPaymentMethodCommand> validator,
    IPaymentMethodRepository methods,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : ICommandHandler<RegisterPaymentMethodCommand, PaymentMethodResponse>
{
    public async Task<WaggoResponse<PaymentMethodResponse>> HandleAsync(
        RegisterPaymentMethodCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        WaggoResponse<PaymentMethodResponse> response = new();

        response.ConcatStacks(await validator.ValidateToResponseAsync(command, cancellationToken));
        if (!response.IsValid)
        {
            return response;
        }

        IReadOnlyList<PaymentMethod> existing = await methods.ListByOwnerAsync(currentUser.Id, cancellationToken);
        bool isDefault = command.IsDefault || existing.Count == 0;

        WaggoResponse<PaymentMethod> method = PaymentMethod.Register(
            currentUser.Id,
            command.Brand,
            command.LastFour,
            command.ExpiryMonth,
            command.ExpiryYear,
            isDefault,
            timeProvider.GetUtcNow());
        response.ConcatStacks(method);
        if (!response.IsValid)
        {
            return response;
        }

        await methods.AddAsync(method.Data, cancellationToken);
        response.Data = PaymentMethodResponse.From(method.Data);
        return response;
    }
}
