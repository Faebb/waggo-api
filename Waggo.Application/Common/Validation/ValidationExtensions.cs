using FluentValidation;
using FluentValidation.Results;
using Waggo.Domain.Common;

namespace Waggo.Application.Common.Validation;

/// <summary>Bridges FluentValidation and <see cref="WaggoResponse{T}"/>.</summary>
public static class ValidationExtensions
{
    /// <summary>
    /// Runs the validator and returns a response with one error per failure (code and message of the rule).
    /// Use it as the first step of every handler:
    /// <c>response.ConcatStacks(await validator.ValidateToResponseAsync(...))</c>.
    /// </summary>
    public static async Task<WaggoResponse<T>> ValidateToResponseAsync<T>(
        this IValidator<T> validator,
        T instance,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(validator);

        ValidationResult result = await validator.ValidateAsync(instance, cancellationToken);
        WaggoResponse<T> response = new() { Data = instance };

        foreach (ValidationFailure failure in result.Errors)
        {
            response.AddError(CodeFor(failure), failure.ErrorMessage);
        }

        return response;
    }

    /// <summary>Uses a catalog <see cref="Error"/> (code + Spanish message) for the rule.</summary>
    public static IRuleBuilderOptions<T, TProperty> WithError<T, TProperty>(
        this IRuleBuilderOptions<T, TProperty> rule,
        Error error)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(error);

        return rule.WithErrorCode(error.Code).WithMessage(error.Message);
    }

    // Rules without WithError keep FluentValidation's default code (e.g. "NotEmptyValidator");
    // it is replaced by a stable "Validation.<Property>" code.
    private static string CodeFor(ValidationFailure failure) =>
        failure.ErrorCode.EndsWith("Validator", StringComparison.Ordinal)
            ? $"Validation.{failure.PropertyName}"
            : failure.ErrorCode;
}
