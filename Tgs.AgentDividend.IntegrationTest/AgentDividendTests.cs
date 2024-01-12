using AutoFixture;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Tgs.Agent.Client;
using Tgs.Agent.Client.Models;
using Tgs.Agent.Dividend.Client;
using Tgs.Agent.Dividend.Core.Distributions;
using Tgs.Agent.Dividend.Domain.Enums;
using Tgs.Agent.Dividend.Domain.Interfaces;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;
using Tgs.Agent.Dividend.Domain.Models.AgentContract;
using Tgs.Agent.Dividend.Repository.Entities;
using CreateAgentContractRequest = Tgs.Agent.Dividend.Client.Models.CreateAgentContractRequest;

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
        var brandContractId = Guid.NewGuid();
        var fixture = new Fixture();

        var agents = CreateAgentFixture(agentId).CreateMany<AgentModel>(1);

        //fakeDbContext.Brands.Add(CreateBrandFixture(brandId).Create<Brand>());
        //fakeDbContext.Agents.Add(CreateAgentFixture(agentId).Create<Repository.Entities.Agent>());
        //fakeDbContext.AgentDividendContracts.Add(CreateAgentDividendContractFixture(agentId).Create<AgentDividendContract>());
        //fakeDbContext.PlayerAggregates.Add(CreateAgentAggregateFixture(brandId, agentId).Create<PlayerAggregate>());

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
                new AgentDividendContractConditionModel
                {
                    Rank = 1,
                    NetProfit = 1,
                    ValidNumber = 1,
                    WinLossAmount = 1,
                    Percentage = 10
                }
            }.ToAsyncEnumerable());

        //fakeDividendCalculator
        //    .GetAgentPlayerAggregateAsync(Arg.Any<int>(),
        //        Arg.Any<BrandModel>(),
        //        Arg.Any<AgentDividendContractModel>(),
        //        Arg.Any<CancellationToken>())
        //    .Returns(CreateAgentAggregateFixture(brandId, agentId)
        //        .CreateMany<AgentPlayerAggregateModel>(1)
        //        .ToAsyncEnumerable());

        //fakeDividendCalculator
        //    .GenerateMultipleLayerAsync(
        //        Arg.Any<BrandModel>(),
        //        Arg.Any<AgentDividendContractModel>(),
        //        Arg.Any<AgentPlayerAggregateModel>(),
        //        Arg.Any<CancellationToken>())
        //    .Returns(CreateMultipleLayerFixture(agentId)
        //        .CreateMany<AgentDividendMultipleLayerModel>(1)
        //        .ToAsyncEnumerable());

        //fakeDividendCalculator.GenerateMultipleLayerAsync().Returns()

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

        await sut.GetAgentDividendBeforehandAsync(brandId,agentId, default);

        _ = fakeRepository.ReceivedWithAnyArgs(1).GetAgentDividendsAsync(default, default, default);
    }

    [Test]
    public async Task Get_Agent_Contract()
    {
        var fakeDbContext = Substitute.For<tgsContext>();
        var fakeContractRepository = Substitute.For<IContractRepository>();

        var brandId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var agentContractId = Guid.NewGuid();

        fakeContractRepository.GetAgentDividendContractsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new AgentDividendContractViewModel[]
            {
                new()
                {
                    Id = agentContractId,
                    BrandId = brandId,
                    AgentId = agentId,
                    Name = "name",
                    ContractDividendType = (short)ContractDividendType.OnePercentage,
                    ContractStatus = (short)ContractStatus.Enabled,
                    ValidBetAmount = 1,
                    DepositAmount = 1,
                    BetDays = 1,
                    Conditions = new AgentDividendContractConditionViewModel[]
                    {
                        new()
                        {
                            AgentDividendContractId = agentContractId,
                            Rank = 1,
                            ValidNumber = 1,
                            NetProfit = 1,
                            WinLossAmount = 1,
                            Percentage = 1
                        }
                    }
                }
            }.ToAsyncEnumerable());

        await using var web = new TestAgentDividendApplication(builder => builder.ConfigureServices(services =>
        {
            services.AddSingleton(fakeDbContext)
                .AddSingleton(fakeContractRepository);
        }));

        var httpClient = web.CreateClient();

        var sut = new AgentContractClient(httpClient);

        var response = await sut.GetAgentContractAsync(agentId, default).ToArrayAsync();

        Assert.That(response.Length, Is.GreaterThan(0));
    }

    private Fixture CreateBrandFixture(Guid brandId)
    {
        var fixture = new Fixture();
        fixture.Customize<Brand>(u => u
            .With(s => s.Id, brandId)
            .With(s => s.Agents, new Repository.Entities.Agent[]
            {
            })
            .With(s => s.Players, new Repository.Entities.Player[]
            {
            })
            .With(s => s.PlayerAgentRelations, new PlayerAgentRelation[]
            {
            }));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }

    //private Fixture CreateAgentFixture(Guid agentId)
    //{
    //    var fixture = new Fixture();
    //    fixture.Customize<Repository.Entities.Agent>(u => u
    //        .With(s => s.Id, agentId)
    //        .With(e => e.Path, new LTree($"{agentId:N}"))
    //        .With(s => s.Brand, new Brand())
    //        .With(s => s.PlayerAgentRelation, new PlayerAgentRelation())
    //        .With(s => s.Players, new Player[]
    //        {
    //        }));
    //    fixture.Behaviors.Add(new OmitOnRecursionBehavior());
    //    return fixture;
    //}

    private Fixture CreateAgentFixture(Guid agentId)
    {
        var fixture = new Fixture();
        fixture.Customize<AgentModel>(u => u
            .With(s => s.Id, agentId)
            .With(e => e.Path, new LTree($"{agentId:N}")));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }

    private Fixture CreateMultipleLayerFixture(Guid agentId)
    {
        var fixture = new Fixture();
        fixture.Customize<AgentDividendMultipleLayerModel>(u => u
            .With(s => s.AgentId, agentId));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }

    private Fixture CreateAgentDividendContractFixture(Guid brandId,Guid agentId)
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

    private Fixture CreateAgentAggregateFixture(Guid brandId,Guid agentId)
    {
        var fixture = new Fixture();
        fixture.Customize<AgentPlayerAggregateModel>(u => u
            .With(s => s.BrandId, brandId)
            .With(s => s.AgentId, agentId));
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