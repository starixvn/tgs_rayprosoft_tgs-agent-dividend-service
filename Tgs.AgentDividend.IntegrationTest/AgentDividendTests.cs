using AutoFixture;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Tgs.Agent.Client;
using Tgs.Agent.Client.Models;
using Tgs.Agent.Dividend.Client;
using Tgs.Agent.Dividend.Client.Models;
using Tgs.Agent.Dividend.Core.Workers;
using Tgs.Agent.Dividend.Domain.Enums;
using Tgs.Agent.Dividend.Domain.Interfaces;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;
using Tgs.Agent.Dividend.Repository.Entities;
using AgentDividendBeforehand = Tgs.Agent.Dividend.Domain.Models.AgentDividendBeforehand;
using AgentDividendRecordModel = Tgs.Agent.Dividend.Domain.Models.AgentDividendRecordModel;

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
        var parentId = Guid.NewGuid();
        var fixture = new Fixture();

        var agents = new[]
        {
            CreateAgentFixture(agentId,parentId).Create<AgentModel>(),
            CreateAgentFixture(parentId, null).Create<AgentModel>(),
        };

        fakeRepository.GetBrandAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(CreateBrandModelFixture(brandId).Create<BrandModel>());

        fakeRepository.GetAgentDividendBeforehandAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>())
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

        _ = fakeRepository.ReceivedWithAnyArgs(1).GetAgentDividendBeforehandAsync(default, default, default);
    }

    [Test]
    public async Task Get_Pre_Calculate_Agent_Dividend()
    {
        var fakeDbContext = Substitute.For<tgsContext>();
        var fakeRepository = Substitute.For<IRepository>();
        var fakeAgentClient = Substitute.For<IAgentClient>();
        var fakeDividendCalculator = Substitute.For<IDividendCalculator>();

        var brandId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var fixture = new Fixture();

        var agents = new[]
        {
            CreateAgentFixture(agentId,parentId).Create<AgentModel>(),
            CreateAgentFixture(parentId, null).Create<AgentModel>(),
        };

        fakeRepository.GetBrandAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(CreateBrandModelFixture(brandId).Create<BrandModel>());

        fakeRepository.GetNonArchivedBrandIdsAsyncEnumerable(Arg.Any<Guid?>())
            .Returns(new Guid[]{ brandId }.ToAsyncEnumerable());

        fakeRepository.GetAgentDividendBeforehandAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>())
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

        fakeAgentClient.GetAgentInfoAsync(Arg.Any<GetAgentInfoRequest>(), Arg.Any<CancellationToken>())
            .Returns(fixture.Create<AgentInfoModel>());

        await using var web = new TestAgentDividendApplication(builder => builder.ConfigureServices(services =>
        {
            services.AddSingleton(fakeDbContext)
                .AddSingleton(fakeRepository)
                .AddSingleton(fakeAgentClient)
                .AddSingleton(fakeDividendCalculator);
        }));

        var httpClient = web.CreateClient();

        var sut = new AgentDividendClient(httpClient);

        var response = await sut.GetPreCalculateAgentDividend(brandId, default).ToArrayAsync();

        Assert.That(response, Has.Length.EqualTo(2));

        fakeRepository.ReceivedWithAnyArgs(1).RemoveAllMemoryCache(default);
    }

    [Test]
    public async Task Get_Agent_Dividend_Records()
    {
        var fakeRepository = Substitute.For<IRepository>();
        var fakeAgentClient = Substitute.For<IAgentClient>();

        var brandId = Guid.NewGuid();
        var from = DateTime.UtcNow;
        var to = DateTime.UtcNow;
        var fixture = new Fixture();

        fakeRepository.GetBrandAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(CreateBrandModelFixture(brandId).Create<BrandModel>());

        fakeRepository.GetAgentDividendRecordsAsync(Arg.Any<Guid>(),
                Arg.Any<DistributeStatus?>(),
                Arg.Any<DateTime?>(),
                Arg.Any<DateTime?>(),
                Arg.Any<DateTime?>(),
                Arg.Any<DateTime?>(),
                Arg.Any<string?>(),
                Arg.Any<bool?>())
            .Returns(fixture.CreateMany<AgentDividendRecordModel>(1).ToAsyncEnumerable());

        await using var web = new TestAgentDividendApplication(builder => builder.ConfigureServices(services =>
        {
            services
                .AddSingleton(fakeRepository)
                .AddSingleton(fakeAgentClient);
        }));

        var httpClient = web.CreateClient();

        var sut = new AgentDividendClient(httpClient);

        var request = new GetAgentDividendRecords(brandId, null, from, to, null, null, null, null);

        var response = await sut.GetAgentDividendRecordsAsync(request, default).ToArrayAsync();

        Assert.That(response, Has.Length.EqualTo(1));

        _ = fakeRepository.ReceivedWithAnyArgs(1).GetAgentDividendRecordsAsync(default, null, null, null, null, null, null, null);
    }

    [Test]
    public async Task Manual_Send_Agent_Dividend()
    {
        var fakeRepository = Substitute.For<IRepository>();
        var fakeMediator = Substitute.For<IMediator>();
        
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var agentDividendRecordIds = new Guid[] { id1, id2 };

        await using var web = new TestAgentDividendApplication(builder => builder.ConfigureServices(services =>
        {
            services
                .AddSingleton(fakeRepository)
                .AddSingleton(fakeMediator);
        }));

        var httpClient = web.CreateClient();

        var sut = new AgentDividendClient(httpClient);

        await sut.SendAgentDividendsAsync(agentDividendRecordIds,"user","remark", default);
    }


    private static Fixture CreateAgentFixture(Guid agentId,Guid? parentId)
    {
        var fixture = new Fixture();
        fixture.Customize<AgentModel>(u => u
            .With(s => s.Id, agentId)
            .With(s => s.ParentAgentId, parentId)
            .With(e => e.Path, parentId is null ? new LTree($"{agentId:N}") : new LTree($"{parentId:N}.{agentId:N}")));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }

    private static Fixture CreateAgentDividendContractFixture(Guid brandId, Guid agentId)
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

    private static Fixture CreateBrandModelFixture(Guid brandId)
    {
        var fixture = new Fixture();
        fixture.Customize<BrandModel>(u => u
            .With(s => s.Id, brandId)
            .With(s => s.AgentDividendType, (short)AgentDividendType.OneLayer));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }
}