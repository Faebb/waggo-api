using Waggo.Application.Common.Interfaces.Payments;
using Waggo.Application.Common.Models.Payments;
using Waggo.Domain.Common;

namespace Waggo.Application.UnitTests.TestData;

/// <summary>Doubles for the payment gateway (RF-015 – RF-018).</summary>
internal static class PaymentMother
{
    /// <summary>A gateway that holds every amount with reference <c>sim_hold_1</c>.</summary>
    public static IPaymentGateway ApprovingGateway()
    {
        IPaymentGateway gateway = Substitute.For<IPaymentGateway>();
        gateway.HoldAsync(Arg.Any<PaymentHold>(), Arg.Any<CancellationToken>())
            .Returns(new WaggoResponse<string> { Data = "sim_hold_1" });
        return gateway;
    }
}
