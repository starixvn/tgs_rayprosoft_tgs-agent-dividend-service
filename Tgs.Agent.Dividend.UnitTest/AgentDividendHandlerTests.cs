using System.Text.Json;
using AutoFixture;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Tgs.Agent.Dividend.Core.AgentDividend;
using Tgs.Agent.Dividend.Core.Distributions;
using Tgs.Agent.Dividend.Core.Workers;
using Tgs.Agent.Dividend.Domain.Enums;
using Tgs.Agent.Dividend.Domain.Interfaces;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;
using Tgs.Agent.Dividend.Repository.Entities;

namespace Tgs.Agent.Dividend.UnitTest;

public class AgentDividendHandlerTests
{
    [Test]
    public async Task Generate_Agent_Player_Dividends()
    {
        var fakeMediator = Substitute.For<IMediator>();
        var fakeRepository = Substitute.For<IRepository>();
        var fakeContractRepository = Substitute.For<IContractRepository>();
        var fakeTgsContext = Substitute.For<tgsContext>();
        var fakeMemoryCache = Substitute.For<IMemoryCache>();
        var fakeDistribution = Substitute.For<IDividendCalculator>();

        var services = new ServiceCollection();
        services.AddKeyedSingleton<IDividendCalculator, OneLayerDividendCalculator>(ContractDividendType.OneLayer
            .ToString());
        services.AddSingleton(fakeDistribution)
            .AddSingleton(fakeRepository)
            .AddSingleton(fakeContractRepository)
            .AddSingleton(fakeTgsContext)
            .AddSingleton(fakeMemoryCache)
            .AddSingleton(fakeMediator);

        var sp = services.BuildServiceProvider();

        var brandId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var agentId2 = Guid.NewGuid();

        var from = DateTime.Parse("2023/12/06 00:00:00+8");
        var to = DateTime.Parse("2023/12/06 00:00:00+8");

        fakeRepository.GetNonArchivedBrandIdsAsyncEnumerable(Arg.Any<Guid?>())
            .Returns(new[] { Guid.NewGuid() }.ToAsyncEnumerable());

        var contracts = new[]
        {
            CreateContractFixture(brandId, agentId, ContractDividendType.OneLayer, 0, 0, 0)
                .Create<AgentDividendContractModel>(),
            CreateContractFixture(brandId, agentId2, ContractDividendType.OneLayer, 0, 0, 0)
                .Create<AgentDividendContractModel>()
        };

        var brand = CreateBrandFixture(brandId, from, to, 0, true, AgentDividendType.OneLayer).Create<BrandModel>();

        var agents = new[]
        {
            CreateAgentFixture(brandId, agentId2, agentId, 10, new LTree($"{agentId:N}.{agentId2:N}"))
                .Create<AgentModel>(),
            CreateAgentFixture(brandId, agentId, null, 50, new LTree($"{agentId:N}")).Create<AgentModel>()
        };

        var agentPath = new[]
        {
            new AgentDividendPercentageData
            {
                AgentId = agentId,
                ParentAgentId = null,
                DividendPercentage = 50
            }
        };

        var agent2Path = agentPath.Append(new AgentDividendPercentageData
        {
            AgentId = agentId2,
            ParentAgentId = agentId,
            DividendPercentage = 10
        });

        var agentPlayerAggregates = new[]
        {
            CreateAgentPlayerAggregateFixture(brandId,
                    agentId2,
                    from,
                    to,
                    100,
                    10,
                    JsonSerializer.Serialize(agent2Path))
                .Create<AgentPlayerAggregateModel>()
        };

        fakeContractRepository.GetBrandAgentDividendContractsAsync(Arg.Any<Guid>())
            .Returns(contracts.ToAsyncEnumerable());

        fakeRepository.GetBrandAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(brand);
        fakeRepository.GetMaxAgentDividendIdAsync(Arg.Any<CancellationToken>()).Returns(99);
        fakeRepository.GetAgentsCacheAsyncEnumerable(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(agents.ToAsyncEnumerable());

        fakeRepository
            .GetAgentPlayerAggregatesCacheAsync(brandId, from, to, Arg.Any<CancellationToken>())
            .Returns(agentPlayerAggregates.ToAsyncEnumerable());

        fakeMediator.Send(Arg.Any<GetNoSendDividendRequest>())
            .Returns(new NoSendDividendResponse
            {
                Model = Enumerable.Empty<AgentDividendModel>(),
                NegativeDividendsIds = new Dictionary<int, int> { { 1, 1 } }
            });

        var @event = new AgentDividendRequest(brandId);

        var sut = new BrandAgentDividendHandler(fakeRepository,
            fakeContractRepository,
            fakeMediator,
            sp,
            new NullLogger<BrandAgentDividendHandler>());

        await sut.Handle(@event, default);

        _ = fakeMediator.Received(1).Send(Arg.Any<GetNoSendDividendRequest>(), Arg.Any<CancellationToken>());

        fakeRepository.Received(1).RemoveAllMemoryCache(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Create_Agent_Player_Dividends()
    {
        var fakeMediator = Substitute.For<IMediator>();
        var fakeRepository = Substitute.For<IRepository>();
        var fakeBackgroundTaskQueue = Substitute.For<IBackgroundTaskQueue>();

        var services = new ServiceCollection();
        services.AddKeyedSingleton<IDividendCalculator, OneLayerDividendCalculator>(ContractDividendType.OneLayer
            .ToString());
        services
            .AddSingleton(fakeRepository)
            .AddSingleton(fakeMediator)
            .AddSingleton(fakeBackgroundTaskQueue);

        var brandId = Guid.NewGuid();

        var from = DateTime.Parse("2023/12/06 00:00:00+8");
        var to = DateTime.Parse("2023/12/06 00:00:00+8");

        var brand = CreateBrandFixture(brandId, from, to, 0, true, AgentDividendType.OneLayer).Create<BrandModel>();

        fakeRepository.GetBrandAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(brand);

        fakeMediator.Send(Arg.Any<GetNoSendDividendRequest>())
            .Returns(new NoSendDividendResponse
            {
                Model = Enumerable.Empty<AgentDividendModel>(),
                NegativeDividendsIds = new Dictionary<int, int> { { 1, 1 } }
            });

        var request = new CreateAgentDividendRequest(brandId,
            Enumerable.Empty<AgentDividendModel>(),
            new Dictionary<int, int> { { 1, 1 } });

        var sut = new CreateAgentDividendHandler(
            fakeRepository,
            fakeBackgroundTaskQueue,
            fakeMediator);

        await sut.Handle(request, default);

        _ = fakeRepository.ReceivedWithAnyArgs(1).CreateAgentDividendsAsync(null!, null!, null!, default);
    }

    [TestCase(false)]
    [TestCase(true)]
    [Test]
    public async Task Create_Agent_Player_Dividends_For_Multiple_Layer(bool checkDividendActiveCount)
    {
        var fakeMediator = Substitute.For<IMediator>();
        var fakeRepository = Substitute.For<IRepository>();
        var fakeContractRepository = Substitute.For<IContractRepository>();
        var fakeTgsContext = Substitute.For<tgsContext>();
        var fakeMemoryCache = Substitute.For<IMemoryCache>();
        var fakeDistribution = Substitute.For<IDividendCalculator>();

        var services = new ServiceCollection();
        services.AddKeyedSingleton<IDividendCalculator, MultipleLayerDividendCalculator>(
            ContractDividendType.MultipleLayer.ToString());
        services.AddSingleton(fakeDistribution)
            .AddSingleton(fakeRepository)
            .AddSingleton(fakeContractRepository)
            .AddSingleton(fakeTgsContext)
            .AddSingleton(fakeMemoryCache)
            .AddSingleton(fakeMediator);

        var sp = services.BuildServiceProvider();

        var brandId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var agentId2 = Guid.NewGuid();

        var from = DateTime.Parse("2023/12/06 00:00:00+8");
        var to = DateTime.Parse("2023/12/06 00:00:00+8");

        fakeRepository.GetNonArchivedBrandIdsAsyncEnumerable(Arg.Any<Guid?>())
            .Returns(new[] { Guid.NewGuid() }.ToAsyncEnumerable());

        var contracts = new[]
        {
            CreateContractFixture(brandId, agentId, ContractDividendType.MultipleLayer, 0, 0, 0)
                .Create<AgentDividendContractModel>(),
            CreateContractFixture(brandId, agentId2, ContractDividendType.MultipleLayer, 0, 0, 0)
                .Create<AgentDividendContractModel>()
        };

        var brand = CreateBrandFixture(brandId, from, to, 0, checkDividendActiveCount, AgentDividendType.MultipleLayer)
            .Create<BrandModel>();

        var agents = new[]
        {
            CreateAgentFixture(brandId, agentId2, agentId, 10, new LTree($"{agentId:N}.{agentId2:N}"))
                .Create<AgentModel>(),
            CreateAgentFixture(brandId, agentId, null, 50, new LTree($"{agentId:N}")).Create<AgentModel>()
        };

        var agentPath = new[]
        {
            new AgentDividendPercentageData
            {
                AgentId = agentId,
                ParentAgentId = null,
                DividendPercentage = 0
            }
        };

        var agent2Path = agentPath.Append(new AgentDividendPercentageData
        {
            AgentId = agentId2,
            ParentAgentId = agentId,
            DividendPercentage = 0
        });

        var agentPlayerAggregates = new[]
        {
            CreateAgentPlayerAggregateFixture(brandId,
                    agentId2,
                    from,
                    to,
                    100,
                    10,
                    JsonSerializer.Serialize(agent2Path))
                .Create<AgentPlayerAggregateModel>()
        };

        fakeContractRepository.GetBrandAgentDividendContractsAsync(Arg.Any<Guid>())
            .Returns(contracts.ToAsyncEnumerable());

        fakeRepository.GetBrandAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(brand);
        fakeRepository.GetMaxAgentDividendIdAsync(Arg.Any<CancellationToken>()).Returns(99);
        fakeRepository.GetAgentsCacheAsyncEnumerable(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(agents.ToAsyncEnumerable());
        fakeContractRepository.GetAgentDividendContractConditionsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new AgentDividendContractConditionModel
                {
                    Rank = 1,
                    ValidNumber = 1,
                    WinLossAmount = 0,
                    NetProfit = 10,
                    Percentage = 50
                },
                new AgentDividendContractConditionModel
                {
                    Rank = 2,
                    ValidNumber = 2,
                    WinLossAmount = 0,
                    NetProfit = 20,
                    Percentage = 45
                }
            }.ToAsyncEnumerable());
        fakeRepository.GetAgentPlayerBetCountsCacheAsync(Arg.Any<Guid>(),
                Arg.Any<DateTime>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new AgentPlayerBetDaysModel
                {
                    AgentId = agentId2,
                    PlayerCode = "playerCode",
                    DayCount = 1
                }
            }.ToAsyncEnumerable());

        fakeRepository
            .GetAgentPlayerAggregatesForMultipleLayerCacheAsync(brandId, from, to, Arg.Any<CancellationToken>(), false)
            .Returns(agentPlayerAggregates.ToAsyncEnumerable());

        fakeMediator.Send(Arg.Any<GetNoSendDividendRequest>())
            .Returns(new NoSendDividendResponse
            {
                Model = Enumerable.Empty<AgentDividendModel>(),
                NegativeDividendsIds = new Dictionary<int, int> { { 1, 1 } }
            });
        var @event = new AgentDividendRequest(brandId);

        var sut = new BrandAgentDividendHandler(fakeRepository,
            fakeContractRepository,
            fakeMediator,
            sp,
            new NullLogger<BrandAgentDividendHandler>());

        await sut.Handle(@event, default);

        _ = fakeMediator.Received(1).Send(Arg.Any<GetNoSendDividendRequest>(), Arg.Any<CancellationToken>());

        if (checkDividendActiveCount)
            _ = fakeMediator.Received(1)
                .Send(Arg.Is<GetNoSendDividendRequest>(x =>
                        x.AgentDividends.Any(a => a.AgentId == agentId2 && a.AgentDividendAmount == 50)),
                    Arg.Any<CancellationToken>());

        if (!checkDividendActiveCount)
            _ = fakeMediator.Received(1)
                .Send(Arg.Is<GetNoSendDividendRequest>(x =>
                        x.AgentDividends.Any(a => a.AgentId == agentId2 && a.AgentDividendAmount == 45)),
                    Arg.Any<CancellationToken>());
    }

    [TestCase(false)]
    [TestCase(true)]
    [Test]
    public async Task Create_Agent_Player_Dividends_For_One_Percentage(bool checkDividendActiveCount)
    {
        var fakeMediator = Substitute.For<IMediator>();
        var fakeRepository = Substitute.For<IRepository>();
        var fakeContractRepository = Substitute.For<IContractRepository>();
        var fakeTgsContext = Substitute.For<tgsContext>();
        var fakeMemoryCache = Substitute.For<IMemoryCache>();
        var fakeDistribution = Substitute.For<IDividendCalculator>();

        var services = new ServiceCollection();
        services.AddKeyedSingleton<IDividendCalculator, OnePercentageDividendCalculator>(
            ContractDividendType.OnePercentage.ToString());
        services.AddSingleton(fakeDistribution)
            .AddSingleton(fakeRepository)
            .AddSingleton(fakeContractRepository)
            .AddSingleton(fakeTgsContext)
            .AddSingleton(fakeMemoryCache)
            .AddSingleton(fakeMediator);

        var sp = services.BuildServiceProvider();

        var brandId = Guid.NewGuid();
        var agentId = Guid.Parse("50f5147a-4ddb-40f7-92e8-5610444d9843");
        var agentId2 = Guid.Parse("a98e1bd1-8101-0000-0001-000007f21587");

        var from = DateTime.Parse("2023/12/06 00:00:00+8");
        var to = DateTime.Parse("2023/12/06 00:00:00+8");

        fakeRepository.GetNonArchivedBrandIdsAsyncEnumerable(Arg.Any<Guid?>())
            .Returns(new[] { Guid.NewGuid() }.ToAsyncEnumerable());

        var contracts = new[]
        {
            CreateContractFixture(brandId, agentId, ContractDividendType.OnePercentage, 0, 0, 0)
                .Create<AgentDividendContractModel>(),
            CreateContractFixture(brandId, agentId2, ContractDividendType.OnePercentage, 1, 1, 1)
                .Create<AgentDividendContractModel>()
        };

        var brand = CreateBrandFixture(brandId, from, to, 0, checkDividendActiveCount, AgentDividendType.OnePercentage)
            .Create<BrandModel>();

        var agents = new[]
        {
            CreateAgentFixture(brandId, agentId2, agentId, 0, new LTree($"{agentId:N}.{agentId2:N}"))
                .Create<AgentModel>(),
            CreateAgentFixture(brandId, agentId, null, 0, new LTree($"{agentId:N}")).Create<AgentModel>()
        };

        var agentPath = new[]
        {
            new AgentDividendPercentageData
            {
                AgentId = agentId,
                ParentAgentId = null,
                DividendPercentage = 0
            }
        };

        var agent2Path = agentPath.Append(new AgentDividendPercentageData
        {
            AgentId = agentId2,
            ParentAgentId = agentId,
            DividendPercentage = 0
        });

        var agentPlayerAggregates = new[]
        {
            CreateAgentPlayerAggregateFixture(brandId,
                    agentId2,
                    from,
                    to,
                    100,
                    0,
                    JsonSerializer.Serialize(agent2Path))
                .Create<AgentPlayerAggregateModel>()
        };

        fakeContractRepository.GetBrandAgentDividendContractsAsync(Arg.Any<Guid>())
            .Returns(contracts.ToAsyncEnumerable());

        fakeRepository.GetBrandAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(brand);
        fakeRepository.GetMaxAgentDividendIdAsync(Arg.Any<CancellationToken>()).Returns(99);
        fakeRepository.GetAgentsCacheAsyncEnumerable(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(agents.ToAsyncEnumerable());
        fakeRepository
            .GetDescendantAgentsByPathCacheAsyncEnumerable(Arg.Any<Guid>(),
                Arg.Any<LTree>(),
                Arg.Any<CancellationToken>())
            .Returns(agents.ToAsyncEnumerable());
        fakeRepository.GetAgentPlayerValidBetsCacheAsync(Arg.Any<Guid>(),
                Arg.Any<DateTime>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new AgentPlayerValidBetModel
                {
                    AgentId = agentId2,
                    PlayerCode = "playerCode",
                    ValidBetAmount = 1
                }
            }.ToAsyncEnumerable());
        fakeRepository.GetAgentPlayerBetCountsCacheAsync(Arg.Any<Guid>(),
                Arg.Any<DateTime>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new AgentPlayerBetDaysModel
                {
                    AgentId = agentId2,
                    PlayerCode = "playerCode",
                    DayCount = 1
                }
            }.ToAsyncEnumerable());
        fakeRepository.GetAgentPlayerDepositsCacheAsync(Arg.Any<Guid>(),
                Arg.Any<DateOnly>(),
                Arg.Any<DateOnly>(),
                Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new AgentPlayerDepositModel
                {
                    AgentId = agentId2,
                    PlayerCode = "playerCode",
                    DepositAmount = 1
                }
            }.ToAsyncEnumerable());
        fakeContractRepository.GetAgentDividendContractConditionsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new[]
            {
                new AgentDividendContractConditionModel
                {
                    Rank = 1,
                    ValidNumber = 1,
                    WinLossAmount = 10,
                    NetProfit = 0,
                    Percentage = 50
                },
                new AgentDividendContractConditionModel
                {
                    Rank = 2,
                    ValidNumber = 2,
                    WinLossAmount = 20,
                    NetProfit = 0,
                    Percentage = 45
                }
            }.ToAsyncEnumerable());
        fakeRepository
            .GetAgentPlayerAggregatesForMultipleLayerCacheAsync(Arg.Any<Guid>(),
                Arg.Any<DateTime>(),
                Arg.Any<DateTime>(),
                Arg.Any<CancellationToken>())
            .Returns(agentPlayerAggregates.ToAsyncEnumerable());
        fakeRepository.GetAgentPlayerGameAggregatesCacheAsync(Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>())
            .Returns(AsyncEnumerable.Empty<AgentPlayerGameAggregateModel>());
        fakeMediator.Send(Arg.Any<GetNoSendDividendRequest>())
            .Returns(new NoSendDividendResponse
            {
                Model = Enumerable.Empty<AgentDividendModel>(),
                NegativeDividendsIds = new Dictionary<int, int> { { 1, 1 } }
            });
        var @event = new AgentDividendRequest(brandId);

        var sut = new BrandAgentDividendHandler(fakeRepository,
            fakeContractRepository,
            fakeMediator,
            sp,
            new NullLogger<BrandAgentDividendHandler>());

        await sut.Handle(@event, default);

        if (checkDividendActiveCount)
            _ = fakeMediator.Received(1)
                .Send(Arg.Is<GetNoSendDividendRequest>(x =>
                        x.AgentDividends.Any(a => a.AgentId == agentId2 && a.AgentDividendAmount == 50)),
                    Arg.Any<CancellationToken>());

        if (!checkDividendActiveCount)
            _ = fakeMediator.Received(1)
                .Send(Arg.Is<GetNoSendDividendRequest>(x =>
                        x.AgentDividends.Any(a => a.AgentId == agentId2 && a.AgentDividendAmount == 45)),
                    Arg.Any<CancellationToken>());

        _ = fakeMediator.Received(1)
            .Send(Arg.Is<GetNoSendDividendRequest>(x =>
                    x.AgentDividends.Count() == 2),
                Arg.Any<CancellationToken>());
    }

    private Fixture CreateContractFixture(Guid brandId,
        Guid agentId,
        ContractDividendType contractDividendType,
        decimal validBetAmount,
        decimal depositAmount,
        int betDays)
    {
        var fixture = new Fixture();
        fixture.Customize<AgentDividendContractModel>(u => u
            .With(s => s.BrandId, brandId)
            .With(s => s.AgentId, agentId)
            .With(s => s.ContractStatus, 0)
            .With(s => s.ContractDividendType, (short)contractDividendType)
            .With(s => s.DepositAmount, depositAmount)
            .With(s => s.BetDays, betDays)
            .With(s => s.ValidBetAmount, validBetAmount));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }

    [Test]
    public async Task Create_Agent_Player_Dividends_For_One_Layer()
    {
        var fakeMediator = Substitute.For<IMediator>();
        var fakeRepository = Substitute.For<IRepository>();
        var fakeContractRepository = Substitute.For<IContractRepository>();
        var fakeTgsContext = Substitute.For<tgsContext>();
        var fakeMemoryCache = Substitute.For<IMemoryCache>();
        var fakeDistribution = Substitute.For<IDividendCalculator>();

        var services = new ServiceCollection();
        services.AddKeyedSingleton<IDividendCalculator, MultipleLayerDividendCalculator>(
            ContractDividendType.MultipleLayer.ToString());
        services.AddSingleton(fakeDistribution)
            .AddSingleton(fakeRepository)
            .AddSingleton(fakeContractRepository)
            .AddSingleton(fakeTgsContext)
            .AddSingleton(fakeMemoryCache)
            .AddSingleton(fakeMediator);

        var sp = services.BuildServiceProvider();

        var brandId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var agentId2 = Guid.NewGuid();

        var from = DateTime.Parse("2023/12/06 00:00:00+8");
        var to = DateTime.Parse("2023/12/06 00:00:00+8");

        fakeRepository.GetNonArchivedBrandIdsAsyncEnumerable(Arg.Any<Guid?>())
            .Returns(new[] { Guid.NewGuid() }.ToAsyncEnumerable());

        var contracts = new[]
        {
            CreateContractFixture(brandId, agentId, ContractDividendType.MultipleLayer, 0, 0, 0)
                .Create<AgentDividendContractModel>(),
            CreateContractFixture(brandId, agentId2, ContractDividendType.MultipleLayer, 0, 0, 0)
                .Create<AgentDividendContractModel>()
        };

        var brand = CreateBrandFixture(brandId, from, to, 0, true, AgentDividendType.MultipleLayer)
            .Create<BrandModel>();

        var agents = new[]
        {
            CreateAgentFixture(brandId, agentId2, agentId, 10, new LTree($"{agentId:N}.{agentId2:N}"))
                .Create<AgentModel>(),
            CreateAgentFixture(brandId, agentId, null, 50, new LTree($"{agentId:N}")).Create<AgentModel>()
        };

        var agentPath = new[]
        {
            new AgentDividendPercentageData
            {
                AgentId = agentId,
                ParentAgentId = null,
                DividendPercentage = 0
            }
        };

        var agent2Path = agentPath.Append(new AgentDividendPercentageData
        {
            AgentId = agentId2,
            ParentAgentId = agentId,
            DividendPercentage = 0
        });

        var agentPlayerAggregates = new[]
        {
            CreateAgentPlayerAggregateFixture(brandId,
                    agentId2,
                    from,
                    to,
                    100,
                    10,
                    JsonSerializer.Serialize(agent2Path))
                .Create<AgentPlayerAggregateModel>()
        };

        fakeContractRepository.GetBrandAgentDividendContractsAsync(Arg.Any<Guid>())
            .Returns(contracts.ToAsyncEnumerable());

        fakeRepository.GetBrandAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(brand);
        fakeRepository.GetMaxAgentDividendIdAsync(Arg.Any<CancellationToken>()).Returns(99);
        fakeRepository.GetAgentsCacheAsyncEnumerable(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(agents.ToAsyncEnumerable());

        fakeRepository
            .GetAgentPlayerAggregatesCacheAsync(brandId, from, to, Arg.Any<CancellationToken>())
            .Returns(agentPlayerAggregates.ToAsyncEnumerable());

        fakeMediator.Send(Arg.Any<GetNoSendDividendRequest>())
            .Returns(new NoSendDividendResponse
            {
                Model = Enumerable.Empty<AgentDividendModel>(),
                NegativeDividendsIds = new Dictionary<int, int> { { 1, 1 } }
            });
        var @event = new AgentDividendRequest(brandId);

        var sut = new BrandAgentDividendHandler(fakeRepository,
            fakeContractRepository,
            fakeMediator,
            sp,
            new NullLogger<BrandAgentDividendHandler>());

        await sut.Handle(@event, default);

        _ = fakeMediator.Received(1).Send(Arg.Any<GetNoSendDividendRequest>(), Arg.Any<CancellationToken>());
    }


    private static Fixture CreateBrandFixture(Guid brandId,
        DateTime from,
        DateTime to,
        decimal platformCharge,
        bool checkDividendActiveCount,
        AgentDividendType agentDividendType)
    {
        var fixture = new Fixture();
        fixture.Customize<BrandModel>(u => u
            .With(s => s.Id, brandId)
            .With(s => s.SettlementIntervalType, (short)SettlementIntervalType.Daily)
            .With(s => s.CurrentSettlementFrom, from)
            .With(s => s.CurrentSettlementTo, to)
            .With(s => s.PlatformCharge, platformCharge)
            .With(s => s.CheckDividendActiveCount, checkDividendActiveCount)
            .With(s => s.AgentDividendType, (short)agentDividendType));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }

    private static Fixture CreateAgentFixture(Guid brandId,
        Guid agentId,
        Guid? parentId,
        decimal dividendPercentage,
        LTree path)
    {
        var fixture = new Fixture();
        fixture.Customize<AgentModel>(u => u
            .With(s => s.Id, agentId)
            .With(s => s.ParentAgentId, parentId)
            .With(s => s.DividendPercentage, dividendPercentage)
            .With(s => s.Path, path)
            .With(s => s.BrandId, brandId));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }

    private static Fixture CreateAgentPlayerAggregateFixture(Guid brandId,
        Guid agentId,
        DateTime from,
        DateTime to,
        decimal winLoss,
        decimal agentDividendPercentage,
        string path)
    {
        var fixture = new Fixture();
        fixture.Customize<AgentPlayerAggregateModel>(u => u
            .With(s => s.BrandId, brandId)
            .With(s => s.AgentId, agentId)
            .With(s => s.From, from)
            .With(s => s.To, to)
            .With(s => s.WinLossAmount, winLoss)
            .With(s => s.BonusAmount, 0)
            .With(s => s.AgentAdjustAmount, 0)
            .With(s => s.NoIncludeNetProfitAgentAdjustAmount, 0)
            .With(s => s.BonusAdjustAmount, 0)
            .With(s => s.NoIncludeNetProfitBonusAdjustAmount, 0)
            .With(s => s.AgentDividendPercentage, agentDividendPercentage)
            .With(s => s.AgentDividendPercentageTreeData, path));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }
}