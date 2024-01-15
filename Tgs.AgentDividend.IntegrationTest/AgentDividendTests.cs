using AutoFixture;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Tgs.Agent.Client;
using Tgs.Agent.Client.Models;
using Tgs.Agent.Dividend.Client;
using Tgs.Agent.Dividend.Domain.Enums;
using Tgs.Agent.Dividend.Domain.Interfaces;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;
using Tgs.Agent.Dividend.Repository.Entities;

namespace Tgs.Agent.Dividend.IntegrationTest;

public class AgentDividendTests
{
    [Test]
    public async Task Get_Agent_Dividend_Beforehand()
    {
        var fakeDbContext = Substitute.For<tgsContext>();
        var fakeContractRepository = Substitute.For<IContractRepository>();
        var fakeRepository = Substitute.For<IRepository>();
        var fakeAgentClient = Substitute.For<IAgentClient>();
        var fakeDividendCalculator = Substitute.For<IDividendCalculator>();

        var brandId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var fixture = new Fixture();

        var agents = CreateAgentFixture(agentId).CreateMany<AgentModel>(1);

        fakeRepository.GetBrandAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(CreateBrandModelFixture(brandId).Create<BrandModel>());

        fakeRepository.GetAgentDividendsAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(fixture.CreateMany<AgentDividendBeforehand>(1).ToAsyncEnumerable());

        fakeRepository
            .GetDescendantAgentsByPathCacheAsyncEnumerable(Arg.Any<Guid>(),
                Arg.Any<LTree>(),
                Arg.Any<CancellationToken>())
            .Returns(agents.ToAsyncEnumerable());

        fakeRepository.GetAgentsCacheAsyncEnumerable(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(agents.ToAsyncEnumerable());

        fakeAgentClient.GetChildAgentInfosAsync(Arg.Any<GetChildAgentInfosRequest>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<ChildAgentInfoModel>().ToAsyncEnumerable());

        fakeContractRepository.GetBrandAgentDividendContractsAsync(Arg.Any<Guid>())
            .Returns(CreateAgentDividendContractFixture(brandId, agentId)
                .CreateMany<AgentDividendContractModel>(1)
                .ToAsyncEnumerable());

        fakeContractRepository.GetAgentDividendContractConditionsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new AgentDividendContractConditionModel[]
            {
                new()
                {
                    Rank = 1,
                    NetProfit = 1,
                    ValidNumber = 1,
                    WinLossAmount = 1,
                    Percentage = 10
                }
            }.ToAsyncEnumerable());

        await using var web = new TestAgentDividendApplication(builder => builder.ConfigureServices(services =>
        {
            services.AddSingleton(fakeDbContext)
                .AddSingleton(fakeContractRepository)
                .AddSingleton(fakeRepository)
                .AddSingleton(fakeAgentClient)
                .AddSingleton(fakeDividendCalculator);
        }));

        var httpClient = web.CreateClient();

        var sut = new AgentDividendClient(httpClient);

        await sut.GetAgentDividendBeforehandAsync(brandId, agentId, default);

        _ = fakeRepository.ReceivedWithAnyArgs(1).GetAgentDividendsAsync(default, default, default);
    }

    private Fixture CreateAgentFixture(Guid agentId)
    {
        var fixture = new Fixture();
        fixture.Customize<AgentModel>(u => u
            .With(s => s.Id, agentId)
            .With(e => e.Path, new LTree($"{agentId:N}")));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }

    private Fixture CreateAgentDividendContractFixture(Guid brandId, Guid agentId)
    {
        var fixture = new Fixture();
        fixture.Customize<AgentDividendContractModel>(u => u
            .With(s => s.BrandId, brandId)
            .With(s => s.AgentId, agentId)
            .With(s => s.ContractStatus, (short)ContractStatus.Enabled)
            .With(s => s.ContractDividendType, (short)ContractDividendType.OnePercentage));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }

    private Fixture CreateBrandModelFixture(Guid brandId)
    {
        var fixture = new Fixture();
        fixture.Customize<BrandModel>(u => u
            .With(s => s.Id, brandId)
            .With(s => s.AgentDividendType, (short)AgentDividendType.OnePercentage));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }
}