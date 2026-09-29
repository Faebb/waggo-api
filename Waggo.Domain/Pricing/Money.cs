namespace Waggo.Domain.Pricing;

/// <summary>Immutable amount of money in a given ISO-4217 currency.</summary>
public sealed record Money
{
    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }

    public string Currency { get; }

    public static Money Of(decimal amount, string currency)
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), amount, "Money cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(currency) || currency.Trim().Length != 3)
        {
            throw new ArgumentException("Currency must be a 3-letter ISO-4217 code.", nameof(currency));
        }

        return new Money(amount, currency.Trim().ToUpperInvariant());
    }

    public static Money Zero(string currency) => Of(0m, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return Of(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal factor) => Of(Amount * factor, Currency);

    /// <summary>Rounds to the nearest multiple of <paramref name="increment"/> (half away from zero).</summary>
    public Money RoundToNearest(decimal increment)
    {
        if (increment <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(increment), increment, "Increment must be positive.");
        }

        decimal rounded = Math.Round(Amount / increment, MidpointRounding.AwayFromZero) * increment;
        return new Money(rounded, Currency);
    }

    public override string ToString() => $"{Amount:0.##} {Currency}";

    private void EnsureSameCurrency(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (other.Currency != Currency)
        {
            throw new InvalidOperationException($"Cannot combine {Currency} with {other.Currency}.");
        }
    }
}
