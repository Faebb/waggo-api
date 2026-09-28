using System.Reflection;
using NetArchTest.Rules;

namespace Waggo.ArchitectureTests;

/// <summary>Guards the Clean Architecture dependency rule (RNF-009). If one of these fails, the design was broken.</summary>
public class LayerTests
{
    private const string DomainNs = "Waggo.Domain";
    private const string ApplicationNs = "Waggo.Application";
    private const string InfrastructureNs = "Waggo.Infrastructure";
    private const string ApiNs = "Waggo.Api";

    private static readonly Assembly Domain = typeof(Waggo.Domain.Common.WaggoResponse).Assembly;
    private static readonly Assembly Application = typeof(Waggo.Application.DependencyInjection).Assembly;
    private static readonly Assembly Infrastructure = typeof(Waggo.Infrastructure.DependencyInjection).Assembly;

    [Fact]
    public void Domain_DoesNotDependOnOtherLayersOrFrameworks()
    {
        var result = Types.InAssembly(Domain)
            .ShouldNot()
            .HaveDependencyOnAny(ApplicationNs, InfrastructureNs, ApiNs, "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    [Fact]
    public void Application_DoesNotDependOnInfrastructureOrApi()
    {
        var result = Types.InAssembly(Application)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureNs, ApiNs, "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    [Fact]
    public void Infrastructure_DoesNotDependOnApi()
    {
        var result = Types.InAssembly(Infrastructure)
            .ShouldNot()
            .HaveDependencyOn(ApiNs)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    [Fact]
    public void Handlers_AreSealedAndNotPublic()
    {
        var result = Types.InAssembly(Application)
            .That().HaveNameEndingWith("Handler")
            .And().AreClasses()
            .Should().BeSealed()
            .And().NotBePublic()
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    [Fact]
    public void Domain_NeverStoresCardData_Rnf004()
    {
        string[] forbidden = ["CardNumber", "Pan", "Cvv", "Cvc", "ExpirationDate"];

        var offenders = Domain.GetTypes()
            .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            .Where(p => forbidden.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
            .Select(p => $"{p.DeclaringType?.Name}.{p.Name}")
            .ToList();

        offenders.ShouldBeEmpty();
    }

    private static string Describe(TestResult result) =>
        "Violations: " + string.Join(", ", result.FailingTypeNames ?? []);
}
