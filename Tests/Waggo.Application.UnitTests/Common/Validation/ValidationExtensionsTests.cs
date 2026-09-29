using FluentValidation;
using Waggo.Application.Common.Validation;
using Waggo.Domain.Common;

namespace Waggo.Application.UnitTests.Common.Validation;

public class ValidationExtensionsTests
{
    private static readonly Error s_nameRequired = new("Test.NameRequired", "El nombre es obligatorio.");

    [Fact]
    public async Task ValidateToResponseAsync_ValidInstance_IsValidWithData()
    {
        Person person = new("Ana", 30);

        WaggoResponse<Person> response =
            await new PersonValidator().ValidateToResponseAsync(person, CancellationToken.None);

        response.IsValid.ShouldBeTrue();
        response.Data.ShouldBe(person);
    }

    [Fact]
    public async Task ValidateToResponseAsync_UsesTheCatalogCodeAndMessage()
    {
        WaggoResponse<Person> response =
            await new PersonValidator().ValidateToResponseAsync(new Person("", 30), CancellationToken.None);

        response.Errors.Single().ShouldBe(new WaggoMessage("Test.NameRequired", "El nombre es obligatorio."));
        response.ErrorType.ShouldBe(ErrorType.Validation);
    }

    [Fact]
    public async Task ValidateToResponseAsync_RuleWithoutWithError_GetsValidationPropertyCode()
    {
        WaggoResponse<Person> response =
            await new PersonValidator().ValidateToResponseAsync(new Person("Ana", -1), CancellationToken.None);

        response.Errors.Single().Code.ShouldBe("Validation.Age");
    }

    [Fact]
    public async Task ValidateToResponseAsync_ReportsEveryFailure()
    {
        WaggoResponse<Person> response =
            await new PersonValidator().ValidateToResponseAsync(new Person("", -1), CancellationToken.None);

        response.Errors.Count.ShouldBe(2);
    }

    internal sealed record Person(string Name, int Age);

    private sealed class PersonValidator : AbstractValidator<Person>
    {
        public PersonValidator()
        {
            RuleFor(p => p.Name).NotEmpty().WithError(s_nameRequired);
            RuleFor(p => p.Age).GreaterThanOrEqualTo(0);
        }
    }
}
