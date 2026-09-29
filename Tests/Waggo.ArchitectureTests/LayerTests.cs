using System.Reflection;
using NetArchTest.Rules;

namespace Waggo.ArchitectureTests;

/// <summary>
/// Guards the Clean Architecture dependency rule (RNF-009). If one of these fails, the design was broken.
/// </summary>
public class LayerTests
{
    private const string DomainNs = "Waggo.Domain";
    private const string ApplicationNs = "Waggo.Application";
    private const string InfrastructureNs = "Waggo.Infrastructure";
    private const string ApiNs = "Waggo.Api";

    private static readonly Assembly s_domain = typeof(Waggo.Domain.Common.Error).Assembly;
    private static readonly Assembly s_application = typeof(Waggo.Application.DependencyInjection).Assembly;
    private static readonly Assembly s_infrastructure = typeof(Waggo.Infrastructure.DependencyInjection).Assembly;

    [Fact]
    public void Domain_DoesNotDependOnOtherLayersOrFrameworks()
    {
        TestResult result = Types.InAssembly(s_domain)
            .ShouldNot()
            .HaveDependencyOnAny(
                ApplicationNs, InfrastructureNs, ApiNs, "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    [Fact]
    public void Application_DoesNotDependOnInfrastructureOrApi()
    {
        TestResult result = Types.InAssembly(s_application)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNs, ApiNs, "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    [Fact]
    public void Infrastructure_DoesNotDependOnApi()
    {
        TestResult result = Types.InAssembly(s_infrastructure)
            .ShouldNot()
            .HaveDependencyOn(ApiNs)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    [Fact]
    public void Handlers_AreSealedAndNotPublic()
    {
        TestResult result = Types.InAssembly(s_application)
            .That().HaveNameEndingWith("Handler")
            .And().AreClasses()
            .Should().BeSealed()
            .And().NotBePublic()
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    [Fact]
    public void Validators_AreSealedAndNotPublic()
    {
        TestResult result = Types.InAssembly(s_application)
            .That().HaveNameEndingWith("Validator")
            .And().AreClasses()
            .Should().BeSealed()
            .And().NotBePublic()
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    [Fact]
    public void CustomExceptions_AreSealed_AndLiveInDomain()
    {
        TestResult result = Types.InAssembly(s_domain)
            .That().Inherit(typeof(Waggo.Domain.Common.Exceptions.WaggoException))
            .Should().BeSealed()
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    [Fact]
    public void Domain_NeverStoresCardData_Rnf004()
    {
        string[] forbidden = ["CardNumber", "Pan", "Cvv", "Cvc", "ExpirationDate"];

        List<string> offenders = s_domain.GetTypes()
            .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            .Where(p => forbidden.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
            .Select(p => $"{p.DeclaringType?.Name}.{p.Name}")
            .ToList();

        offenders.ShouldBeEmpty();
    }

    private static string Describe(TestResult result) =>
        "Violations: " + string.Join(", ", result.FailingTypeNames ?? []);
}
