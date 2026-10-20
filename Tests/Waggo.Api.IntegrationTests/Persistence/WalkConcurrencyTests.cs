using Microsoft.Extensions.DependencyInjection;
using Waggo.Api.IntegrationTests.Infrastructure;
using Waggo.Application.Common.Interfaces.Walks;
using Waggo.Domain.Entities.Walks;
using Waggo.Domain.Enums.Pricing;
using Waggo.Domain.Errors.Walks;
using Waggo.Domain.Exceptions;
using Waggo.Domain.ValueObjects.Pricing;
using Waggo.Domain.ValueObjects.Walks;

namespace Waggo.Api.IntegrationTests.Persistence;

/// <summary>RF-007: two walkers accepting the same request at the same moment cannot both win (xmin).</summary>
[Collection(ApiCollectionDefinition.Name)]
public class WalkConcurrencyTests(WaggoApiFactory factory)
{
    [Fact]
    public async Task SaveChanges_TwoWalkersAcceptTheSameLoadedWalk_TheSecondGetsNotAvailable()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Walk requested = Walk.Request(
            $"owner-{Guid.NewGuid():N}",
            [Guid.NewGuid()],
            WalkType.Individual,
            WalkDuration.Create(60).Data,
            "Cra 7 # 45-10, Bogotá",
            GeoPoint.Create(4.6361, -74.0645).Data,
            null,
            null,
            new FareBreakdown(Money.Of(23000m, "COP"), Money.Of(4600m, "COP"), Money.Of(18400m, "COP")),
            now).Data;

        using (IServiceScope setup = factory.Services.CreateScope())
        {
            await setup.ServiceProvider.GetRequiredService<IWalkRepository>().AddAsync(requested, CancellationToken.None);
        }

        // Each walker loads the walk in its own request (scope) before any of them saves.
        using IServiceScope first = factory.Services.CreateScope();
        using IServiceScope second = factory.Services.CreateScope();
        IWalkRepository firstWalks = first.ServiceProvider.GetRequiredService<IWalkRepository>();
        IWalkRepository secondWalks = second.ServiceProvider.GetRequiredService<IWalkRepository>();
        Walk seenByFirst = (await firstWalks.GetAsync(requested.Id, CancellationToken.None))!;
        Walk seenBySecond = (await secondWalks.GetAsync(requested.Id, CancellationToken.None))!;

        seenByFirst.Accept("walker-1", now).IsValid.ShouldBeTrue();
        seenBySecond.Accept("walker-2", now).IsValid.ShouldBeTrue(); // still looks open in its own copy
        await firstWalks.SaveChangesAsync(CancellationToken.None);

        ConflictException conflict =
            await Should.ThrowAsync<ConflictException>(() => secondWalks.SaveChangesAsync(CancellationToken.None));

        conflict.Error.ShouldBe(WalkErrors.NotAvailable);
        using IServiceScope check = factory.Services.CreateScope();
        Walk stored = (await check.ServiceProvider.GetRequiredService<IWalkRepository>()
            .GetAsync(requested.Id, CancellationToken.None))!;
        stored.WalkerId.ShouldBe("walker-1");
    }
}
