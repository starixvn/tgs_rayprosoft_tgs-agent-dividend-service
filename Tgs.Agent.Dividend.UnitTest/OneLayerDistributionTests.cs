using System.Text.Json;
using AutoFixture;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tgs.Agent.Dividend.Core.Distributions;
using Tgs.Agent.Dividend.Domain.Enums;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.UnitTest;

public class OneLayerDistributionTests
{
    [SetUp]
    public void Setup()
    {
    }

    [TestCase(100, 77, 60, 33, 11, 67)]
    [TestCase(100, 90, 60, 33, 22, 80)]
    [TestCase(100, 90, 80, 70, 60, 80)]
    public async Task Generate_Agent_Dividend(
        decimal agent4WinLossAmount,
        decimal agentDividendPercentage,
        decimal agent2DividendPercentage,
        decimal agent3DividendPercentage,
        decimal agent4DividendPercentage,
        decimal dividendAmount)
    {
        var fakeRepository = Substitute.For<IRepository>();
        var fakeContractRepository = Substitute.For<IContractRepository>();

        var sut = new OneLayerDividendCalculator(
            fakeContractRepository,
            fakeRepository);

        var brandId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var agentId2 = Guid.NewGuid();
        var agentId3 = Guid.NewGuid();
        var agentId4 = Guid.NewGuid();
        var from = DateTime.Parse("2023/12/06 00:00:00+8");
        var to = DateTime.Parse("2023/12/06 00:00:00+8");

        var brand = CreateBrandFixture(brandId, from, to, 0, AgentDividendType.OneLayer).Create<BrandModel>();

        var agents = new[]
        {
            CreateAgentFixture(brandId,
                    agentId4,
                    agentId3,
                    new LTree($"{agentId2:N}.{agentId:N}.{agentId3:N}.{agentId4:N}"))
                .Create<AgentModel>(),
            CreateAgentFixture(brandId, agentId3, agentId, new LTree($"{agentId2:N}.{agentId:N}.{agentId3:N}"))
                .Create<AgentModel>(),
            CreateAgentFixture(brandId, agentId, agentId2, new LTree($"{agentId2:N}.{agentId:N}"))
                .Create<AgentModel>(),
            CreateAgentFixture(brandId, agentId2, null, new LTree($"{agentId2:N}")).Create<AgentModel>()
        };

        var agentPath = new[]
        {
            new AgentDividendPercentageData
            {
                AgentId = agentId,
                ParentAgentId = null,
                DividendPercentage = agentDividendPercentage
            }
        };

        var agent2Path = agentPath.Append(new AgentDividendPercentageData
        {
            AgentId = agentId2,
            ParentAgentId = agentId,
            DividendPercentage = agent2DividendPercentage
        });

        var agent3Path = agent2Path.Append(new AgentDividendPercentageData
        {
            AgentId = agentId3,
            ParentAgentId = agentId2,
            DividendPercentage = agent3DividendPercentage
        });

        var agent4Path = agent3Path.Append(new AgentDividendPercentageData
        {
            AgentId = agentId4,
            ParentAgentId = agentId3,
            DividendPercentage = agent4DividendPercentage
        });

        var agentPlayerAggregate = CreateAgentPlayerAggregateFixture(brandId,
                agentId4,
                from,
                to,
                agent4WinLossAmount,
                agent4DividendPercentage,
                JsonSerializer.Serialize(agent4Path))
            .Create<AgentPlayerAggregateModel>();

        agentPlayerAggregate.AgentDividendProductFee =
            CreateAgentDividendProductFeeFixture(agentPlayerAggregate.Id, agentPlayerAggregate.AgentId,
                    0, 0, 0, 0, 0, 0, 10, agent4WinLossAmount, 0, 0, 10, -agent4WinLossAmount)
                .Create<AgentDividendProductFeeModel>();

        var agentPlayerGameAggregates = new[]
        {
            CreateAgentPlayerGameAggregateFixture(brandId, agentId4, from, to)
                .Create<AgentPlayerGameAggregateModel>(),
            CreateAgentPlayerGameAggregateFixture(brandId, agentId4, from, to)
                .Create<AgentPlayerGameAggregateModel>(),
            CreateAgentPlayerGameAggregateFixture(brandId, agentId4, from, to)
                .Create<AgentPlayerGameAggregateModel>(),
            CreateAgentPlayerGameAggregateFixture(brandId, agentId4, from, to)
                .Create<AgentPlayerGameAggregateModel>()
        };

        fakeRepository.GetBrandAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(brand);
        fakeRepository.GetMaxAgentDividendIdAsync(Arg.Any<CancellationToken>()).Returns(99);
        fakeRepository.GetAgentsCacheAsyncEnumerable(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(agents.ToAsyncEnumerable());

        fakeRepository
            .GetAgentPlayerAggregatesCacheAsync(brandId, from, to, Arg.Any<CancellationToken>())
            .Returns(new[] { agentPlayerAggregate }.ToAsyncEnumerable());

        fakeRepository
            .GetAgentPlayerGameAggregatesCacheAsync(brandId, from, to, default)
            .Returns(agentPlayerGameAggregates.ToAsyncEnumerable());

        var response = await sut
            .GenerateMultipleLayerAsync(brand, new AgentModel(), agentPlayerAggregate, false, default)
            .ToArrayAsync();

        Assert.Multiple(() =>
        {
            Assert.That(response, Has.Length.EqualTo(4));
            Assert.That(response.Count(x => x.AgentId == agentId), Is.EqualTo(1));
            Assert.That(response.Count(x => x.AgentId == agentId2), Is.EqualTo(1));
            Assert.That(response.Count(x => x.AgentId == agentId3), Is.EqualTo(1));
            Assert.That(response.Count(x => x.AgentId == agentId4), Is.EqualTo(1));
            Assert.That(response.Sum(x => x.Dividend), Is.EqualTo(dividendAmount));
        });
    }

    [TestCase(100, 77, 17, 60, 27, 33, 22, 11, 1)]
    [TestCase(100, 90, 30, 60, 27, 33, 11, 22, 12)]
    [TestCase(100, 90, 10, 80, 10, 70, 10, 60, 50)]
    public async Task Generate_Agent_Dividend_Difference_Percentage_With_Same_Interval(
        decimal agent4WinLossAmount,
        decimal agentDividendPercentage,
        decimal agentDividendAmount,
        decimal agent2DividendPercentage,
        decimal agent2DividendAmount,
        decimal agent3DividendPercentage,
        decimal agent3DividendAmount,
        decimal agent4FirstDividendPercentage,
        decimal agent4DividendAmount)
    {
        var fakeRepository = Substitute.For<IRepository>();
        var fakeContractRepository = Substitute.For<IContractRepository>();

        var brandId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var agentId2 = Guid.NewGuid();
        var agentId3 = Guid.NewGuid();
        var agentId4 = Guid.NewGuid();
        var from = DateTime.Parse("2023/12/06 00:00:00+8");
        var to = DateTime.Parse("2023/12/06 00:00:00+8");

        var brand = CreateBrandFixture(brandId, from, to, 0, AgentDividendType.OneLayer).Create<BrandModel>();

        var agents = new[]
        {
            CreateAgentFixture(brandId,
                    agentId4,
                    agentId3,
                    new LTree($"{agentId2:N}.{agentId:N}.{agentId3:N}.{agentId4:N}"))
                .Create<AgentModel>(),
            CreateAgentFixture(brandId, agentId3, agentId, new LTree($"{agentId2:N}.{agentId:N}.{agentId3:N}"))
                .Create<AgentModel>(),
            CreateAgentFixture(brandId, agentId, agentId2, new LTree($"{agentId2:N}.{agentId:N}"))
                .Create<AgentModel>(),
            CreateAgentFixture(brandId, agentId2, null, new LTree($"{agentId2:N}")).Create<AgentModel>()
        };

        var agentPath = new[]
        {
            new AgentDividendPercentageData
            {
                AgentId = agentId,
                ParentAgentId = null,
                DividendPercentage = agentDividendPercentage
            }
        };

        var agent2Path = agentPath.Append(new AgentDividendPercentageData
        {
            AgentId = agentId2,
            ParentAgentId = agentId,
            DividendPercentage = agent2DividendPercentage
        });

        var agent3Path = agent2Path.Append(new AgentDividendPercentageData
        {
            AgentId = agentId3,
            ParentAgentId = agentId2,
            DividendPercentage = agent3DividendPercentage
        });

        var agent4FirstPath = agent3Path.Append(new AgentDividendPercentageData
        {
            AgentId = agentId4,
            ParentAgentId = agentId3,
            DividendPercentage = agent4FirstDividendPercentage
        });

        var agentPlayerAggregates = new[]
        {
            CreateAgentPlayerAggregateFixture(brandId,
                    agentId4,
                    from,
                    to,
                    agent4WinLossAmount,
                    agent4FirstDividendPercentage,
                    JsonSerializer.Serialize(agent4FirstPath))
                .Create<AgentPlayerAggregateModel>()
        };

        foreach (var agentPlayerAggregateModel in agentPlayerAggregates)
        {
            agentPlayerAggregateModel.AgentDividendProductFee =
                CreateAgentDividendProductFeeFixture(agentPlayerAggregateModel.Id, agentPlayerAggregateModel.AgentId,
                        0, 0, 0, 0, 0, 0, 10, agent4WinLossAmount, 0, 0, 0, 0)
                    .Create<AgentDividendProductFeeModel>();
        }

        var agentPlayerGameAggregates = new[]
        {
            CreateAgentPlayerGameAggregateFixture(brandId, agentId4, from, to)
                .Create<AgentPlayerGameAggregateModel>(),
            CreateAgentPlayerGameAggregateFixture(brandId, agentId4, from, to)
                .Create<AgentPlayerGameAggregateModel>(),
            CreateAgentPlayerGameAggregateFixture(brandId, agentId4, from, to)
                .Create<AgentPlayerGameAggregateModel>(),
            CreateAgentPlayerGameAggregateFixture(brandId, agentId4, from, to)
                .Create<AgentPlayerGameAggregateModel>()
        };

        fakeRepository.GetBrandAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(brand);
        fakeRepository.GetMaxAgentDividendIdAsync(Arg.Any<CancellationToken>()).Returns(99);
        fakeRepository.GetAgentsCacheAsyncEnumerable(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(agents.ToAsyncEnumerable());

        fakeRepository
            .GetAgentPlayerAggregatesCacheAsync(brandId, from, to, Arg.Any<CancellationToken>())
            .Returns(agentPlayerAggregates.ToAsyncEnumerable());

        fakeRepository
            .GetAgentPlayerGameAggregatesCacheAsync(brandId, from, to, default)
            .Returns(agentPlayerGameAggregates.ToAsyncEnumerable());

        var multipleLayers = new List<AgentDividendMultipleLayerModel>();
        foreach (var agentPlayerAggregateModel in agentPlayerAggregates)
        {
            var sut = new OneLayerDividendCalculator(
                fakeContractRepository,
                fakeRepository);

            multipleLayers.AddRange(await sut
                .GenerateMultipleLayerAsync(brand, new AgentModel(), agentPlayerAggregateModel, false, default)
                .ToArrayAsync());
        }

        Assert.Multiple(() =>
        {
            Assert.That(multipleLayers, Has.Count.EqualTo(4));
            Assert.That(multipleLayers.Count(x => x.AgentId == agentId), Is.EqualTo(1));
            Assert.That(multipleLayers.Count(x => x.AgentId == agentId2), Is.EqualTo(1));
            Assert.That(multipleLayers.Count(x => x.AgentId == agentId3), Is.EqualTo(1));
            Assert.That(multipleLayers.Count(x => x.AgentId == agentId4), Is.EqualTo(1));
            Assert.That(multipleLayers.Where(x => x.AgentId == agentId).Sum(x => x.Dividend),
                Is.EqualTo(agentDividendAmount));
            Assert.That(multipleLayers.Where(x => x.AgentId == agentId2).Sum(x => x.Dividend),
                Is.EqualTo(agent2DividendAmount));
            Assert.That(multipleLayers.Where(x => x.AgentId == agentId3).Sum(x => x.Dividend),
                Is.EqualTo(agent3DividendAmount));
            Assert.That(multipleLayers.Where(x => x.AgentId == agentId4).Sum(x => x.Dividend),
                Is.EqualTo(agent4DividendAmount));
        });
    }

    private static Fixture CreateBrandFixture(Guid brandId,
        DateTime from,
        DateTime to,
        decimal platformCharge,
        AgentDividendType agentDividendType)
    {
        var fixture = new Fixture();
        fixture.Customize<BrandModel>(u => u
            .With(s => s.Id, brandId)
            .With(s => s.CheckDividendActiveCount, true)
            .With(s => s.CurrentSettlementFrom, from)
            .With(s => s.CurrentSettlementTo, to)
            .With(s => s.PlatformCharge, platformCharge)
            .With(s => s.AgentDividendType, (short)agentDividendType));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }

    private static Fixture CreateAgentFixture(Guid brandId, Guid agentId, Guid? parentId, LTree path)
    {
        var fixture = new Fixture();
        fixture.Customize<AgentModel>(u => u
            .With(s => s.Id, agentId)
            .With(s => s.ParentAgentId, parentId)
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
            .With(s => s.BonusAdjustAmount, 0)
            .With(s => s.NoIncludeNetProfitBonusAdjustAmount, 0)
            .With(s => s.AgentDividendPercentage, agentDividendPercentage)
            .With(s => s.AgentDividendPercentageTreeData, path));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }

    private static Fixture CreateAgentPlayerGameAggregateFixture(Guid brandId, Guid agentId, DateTime from, DateTime to)
    {
        var fixture = new Fixture();
        fixture.Customize<AgentPlayerGameAggregateModel>(u => u
            .With(s => s.BrandId, brandId)
            .With(s => s.AgentId, agentId)
            .With(s => s.From, from)
            .With(s => s.To, to));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }

    private static Fixture CreateAgentDividendProductFeeFixture(int agentDividendId, Guid agentId,
        decimal cardGamePercentage, decimal cardGameWinLoss,
        decimal liveDealerPercentage, decimal liveDealerWinLoss,
        decimal lotteryPercentage, decimal lotteryWinLoss,
        decimal slotPercentage, decimal slotWinLoss,
        decimal sportsPercentage, decimal sportsWinLoss,
        decimal virtualSportsPercentage, decimal virtualSportsWinLoss)
    {
        var fixture = new Fixture();
        fixture.Customize<AgentDividendProductFeeModel>(u => u
            .With(s => s.AgentId, agentId)
            .With(s => s.AgentDividendId, agentDividendId)
            .With(s => s.CardGameWinLossAmount, cardGameWinLoss)
            .With(s => s.CurrentCardGamePercentage, cardGamePercentage)
            .With(s => s.LiveDealerWinLossAmount, liveDealerWinLoss)
            .With(s => s.CurrentLiveDealerPercentage, liveDealerPercentage)
            .With(s => s.LotteryWinLossAmount, lotteryWinLoss)
            .With(s => s.CurrentLotteryPercentage, lotteryPercentage)
            .With(s => s.SlotWinLossAmount, slotWinLoss)
            .With(s => s.CurrentSlotPercentage, slotPercentage)
            .With(s => s.SportsWinLossAmount, sportsWinLoss)
            .With(s => s.CurrentSportsPercentage, sportsPercentage)
            .With(s => s.VirtualSportsWinLossAmount, virtualSportsWinLoss)
            .With(s => s.CurrentVirtualSportsPercentage, virtualSportsPercentage));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }
}