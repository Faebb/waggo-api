namespace Waggo.Application.Payments;

/// <summary>What a walker has earned (RF-017). <c>Currency</c> is null while there are no earnings.</summary>
public sealed record WalkerEarningsResponse(string? Currency, decimal Total, IReadOnlyList<EarningResponse> Walks);
