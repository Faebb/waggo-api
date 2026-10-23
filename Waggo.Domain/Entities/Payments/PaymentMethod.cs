using Waggo.Domain.Common;
using Waggo.Domain.Enums.Payments;
using Waggo.Domain.Errors.Payments;

namespace Waggo.Domain.Entities.Payments;

/// <summary>Simulated payment method of an owner (RF-020). Keeps only the brand, the last four digits and the expiry (RNF-004).</summary>
public sealed class PaymentMethod
{
    public const int LastFourLength = 4;
    public const int MaxYearsAhead = 20;

    // Used by EF Core to materialize the entity.
    private PaymentMethod()
    {
    }

    public Guid Id { get; private set; }

    /// <summary>Id of the owner in the identity provider (<c>sub</c> claim).</summary>
    public string OwnerId { get; private set; } = string.Empty;

    public CardBrand Brand { get; private set; }

    public string LastFour { get; private set; } = string.Empty;

    public int ExpiryMonth { get; private set; }

    public int ExpiryYear { get; private set; }

    public bool IsDefault { get; private set; }

    public DateTimeOffset RegisteredAt { get; private set; }

    /// <summary>True when the text is more digits than the last four, so it looks like a full card number.</summary>
    public static bool IsFullCardNumber(string? value)
    {
        string digits = Digits(value);
        return digits.Length > LastFourLength && AllDigits(digits);
    }

    /// <summary>True when the text is exactly four digits (spaces and dashes ignored).</summary>
    public static bool IsValidLastFour(string? value)
    {
        string digits = Digits(value);
        return digits.Length == LastFourLength && AllDigits(digits);
    }

    /// <summary>Creates a payment method, reporting every broken rule at once.</summary>
    public static WaggoResponse<PaymentMethod> Register(
        string ownerId,
        CardBrand brand,
        string lastFour,
        int expiryMonth,
        int expiryYear,
        bool isDefault,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        WaggoResponse<PaymentMethod> response = new();
        DateOnly today = DateOnly.FromDateTime(now.UtcDateTime);

        if (IsFullCardNumber(lastFour))
        {
            response.AddError(PaymentErrors.FullCardNumberNotAllowed);
        }
        else if (!IsValidLastFour(lastFour))
        {
            response.AddError(PaymentErrors.InvalidLastFour);
        }

        if (expiryMonth is < 1 or > 12 || expiryYear > today.Year + MaxYearsAhead)
        {
            response.AddError(PaymentErrors.InvalidExpiry);
        }
        else if (expiryYear < today.Year || (expiryYear == today.Year && expiryMonth < today.Month))
        {
            response.AddError(PaymentErrors.CardExpired);
        }

        if (!response.IsValid)
        {
            return response;
        }

        response.Data = new PaymentMethod
        {
            Id = Guid.CreateVersion7(now),
            OwnerId = ownerId,
            Brand = brand,
            LastFour = Digits(lastFour),
            ExpiryMonth = expiryMonth,
            ExpiryYear = expiryYear,
            IsDefault = isDefault,
            RegisteredAt = now,
        };
        return response;
    }

    private static string Digits(string? value) =>
        string.Concat((value ?? string.Empty).Where(character => character is not (' ' or '-')));

    private static bool AllDigits(string value) => value.All(char.IsAsciiDigit);
}
