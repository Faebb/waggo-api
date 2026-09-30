namespace Waggo.Application.Payments;

/// <summary>The payout of one finished walk.</summary>
public sealed record EarningResponse(Guid WalkId, decimal WalkerPayout, DateTimeOffset CapturedAt);
