using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Tgs.Agent.Dividend.Client;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Repository.Entities;

namespace Tgs.Agent.Dividend.IntegrationTest;

public class GenerateAgentDividendTests
{
    [Test]
    public async Task Create_Agent_Player_Dividends()
    {
        var fakeDbContext = Substitute.For<tgsContext>();
        var fakeRepository = Substitute.For<IRepository>();

        var brandId = Guid.NewGuid();

        fakeRepository.GetNonArchivedBrandIdsAsyncEnumerable(Arg.Any<Guid?>())
            .Returns(new[] { brandId }.ToAsyncEnumerable());

        await using var web = new TestAgentDividendApplication(builder => builder.ConfigureServices(services =>
        {
            services.AddSingleton(fakeDbContext)
                .AddSingleton(fakeRepository);
        }));

        var httpClient = web.CreateClient();

        var sut = new AgentDividendClient(httpClient);

        await sut.GenerateAgentDividend(default);

        _ = fakeRepository.ReceivedWithAnyArgs(1).GetNonArchivedBrandIdsAsyncEnumerable(null!);
    }

    [Test]
    public async Task Create_Agent_Player_Dividends_With_Brand_Id()
    {
        var fakeDbContext = Substitute.For<tgsContext>();
        var fakeRepository = Substitute.For<IRepository>();

        var brandId = Guid.NewGuid();

        fakeRepository.GetNonArchivedBrandIdsAsyncEnumerable(Arg.Any<Guid?>())
            .Returns(new[] { brandId }.ToAsyncEnumerable());

        await using var web = new TestAgentDividendApplication(builder => builder.ConfigureServices(services =>
        {
            services.AddSingleton(fakeDbContext)
                .AddSingleton(fakeRepository);
        }));

        var httpClient = web.CreateClient();

        var sut = new AgentDividendClient(httpClient);

        await sut.GenerateAgentDividend(brandId, default);

        _ = fakeRepository.Received(1).GetNonArchivedBrandIdsAsyncEnumerable(Arg.Is(brandId));
    }
}