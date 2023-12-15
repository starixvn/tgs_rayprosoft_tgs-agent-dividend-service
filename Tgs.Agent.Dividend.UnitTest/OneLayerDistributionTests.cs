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

    [TestCase(100, 77, 60, 33, 11, 77)]
    [TestCase(100, 90, 60, 33, 22, 90)]
    [TestCase(100, 90, 80, 70, 60, 90)]
    public async Task Generate_Agent_Dividend(
        decimal winLossAmount,
        decimal agentDividendPercentage,
        decimal agent2DividendPercentage,
        decimal agent3DividendPercentage,
        decimal agent4DividendPercentage,
        decimal dividendAmount)
    {
        var fakeRepository = Substitute.For<IRepository>();

        var sut = new OneLayerDistribution(
            fakeRepository);

        var brandId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var agentId2 = Guid.NewGuid();
        var agentId3 = Guid.NewGuid();
        var agentId4 = Guid.NewGuid();
        var from = DateTime.Parse("2023/12/06 00:00:00+8");
        var to = DateTime.Parse("2023/12/06 00:00:00+8");

        var contract = CreateContractFixture(brandId,
                agentId4,
                ContractDividendType.MultipleLayer,
                0,
                0,
                0)
            .Create<AgentDividendContractModel>();

        var brand = CreateBrandFixture(brandId, from, to, 0, AgentDividendType.OnePercentage).Create<BrandModel>();

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
                winLossAmount,
                agent4DividendPercentage,
                JsonSerializer.Serialize(agent4Path))
            .Create<AgentPlayerAggregateModel>();

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
            .GetAgentPlayerGameAggregatesCacheAsync(brandId, from, to)
            .Returns(agentPlayerGameAggregates.ToAsyncEnumerable());

        var response = await sut.GenerateMultipleLayerAsync(brand, contract, agentPlayerAggregate, default)
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


    [TestCase(100, 77, 34, 60, 54, 33, 45, 11, 10, 21)]
    [TestCase(100, 90, 60, 60, 54, 33, 24, 22, 20, 42)]
    [TestCase(100, 90, 20, 80, 20, 70, 25, 60, 55, 115)]
    public async Task Generate_Agent_Dividend_Difference_Percentage_With_Same_Interval(
        decimal winLossAmount,
        decimal agentDividendPercentage,
        decimal agentDividendAmount,
        decimal agent2DividendPercentage,
        decimal agent2DividendAmount,
        decimal agent3DividendPercentage,
        decimal agent3DividendAmount,
        decimal agent4FirstDividendPercentage,
        decimal agent4SecondDividendPercentage,
        decimal agent4DividendAmount)
    {
        var fakeRepository = Substitute.For<IRepository>();

        var brandId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var agentId2 = Guid.NewGuid();
        var agentId3 = Guid.NewGuid();
        var agentId4 = Guid.NewGuid();
        var from = DateTime.Parse("2023/12/06 00:00:00+8");
        var to = DateTime.Parse("2023/12/06 00:00:00+8");

        var contract = CreateContractFixture(brandId,
                agentId4,
                ContractDividendType.MultipleLayer,
                0,
                0,
                0)
            .Create<AgentDividendContractModel>();

        var brand = CreateBrandFixture(brandId, from, to, 0, AgentDividendType.OnePercentage).Create<BrandModel>();

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

        var agent4SecondPath = agent3Path.Append(new AgentDividendPercentageData
        {
            AgentId = agentId4,
            ParentAgentId = agentId3,
            DividendPercentage = agent4SecondDividendPercentage
        });

        var agentPlayerAggregates = new[]
        {
            CreateAgentPlayerAggregateFixture(brandId,
                    agentId4,
                    from,
                    to,
                    winLossAmount,
                    agent4FirstDividendPercentage,
                    JsonSerializer.Serialize(agent4FirstPath))
                .Create<AgentPlayerAggregateModel>(),
            CreateAgentPlayerAggregateFixture(brandId,
                    agentId4,
                    from,
                    to,
                    winLossAmount,
                    agent4SecondDividendPercentage,
                    JsonSerializer.Serialize(agent4SecondPath))
                .Create<AgentPlayerAggregateModel>()
        };

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
            .GetAgentPlayerGameAggregatesCacheAsync(brandId, from, to)
            .Returns(agentPlayerGameAggregates.ToAsyncEnumerable());

        var multipleLayers = new List<AgentDividendMultipleLayerModel>();
        foreach (var agentPlayerAggregateModel in agentPlayerAggregates)
        {
            var sut = new OneLayerDistribution(
                fakeRepository);

            multipleLayers.AddRange(await sut
                .GenerateMultipleLayerAsync(brand, contract, agentPlayerAggregateModel, default)
                .ToArrayAsync());
        }

        Assert.Multiple(() =>
        {
            Assert.That(multipleLayers, Has.Count.EqualTo(8));
            Assert.That(multipleLayers.Count(x => x.AgentId == agentId), Is.EqualTo(2));
            Assert.That(multipleLayers.Count(x => x.AgentId == agentId2), Is.EqualTo(2));
            Assert.That(multipleLayers.Count(x => x.AgentId == agentId3), Is.EqualTo(2));
            Assert.That(multipleLayers.Count(x => x.AgentId == agentId4), Is.EqualTo(2));
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

    private static Fixture CreateContractFixture(Guid brandId,
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

    private static Fixture CreateAgentPlayerValidBetFixture(Guid brandId,
        Guid agentId,
        string playerCode,
        DateTime from,
        DateTime to,
        decimal validBet)
    {
        var fixture = new Fixture();
        fixture.Customize<AgentPlayerValidBetModel>(u => u
            .With(s => s.BrandId, brandId)
            .With(s => s.AgentId, agentId)
            .With(s => s.PlayerCode, playerCode)
            .With(s => s.From, from)
            .With(s => s.To, to)
            .With(s => s.ValidBetAmount, validBet));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }

    private static Fixture CreateAgentPlayerBetCountFixture(Guid brandId,
        Guid agentId,
        string playerCode,
        DateTime from,
        DateTime to,
        int count)
    {
        var fixture = new Fixture();
        fixture.Customize<AgentPlayerBetDaysModel>(u => u
            .With(s => s.BrandId, brandId)
            .With(s => s.AgentId, agentId)
            .With(s => s.PlayerCode, playerCode)
            .With(s => s.From, from)
            .With(s => s.To, to)
            .With(s => s.DayCount, count));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }

    private static Fixture CreateAgentPlayerDepositFixture(Guid brandId,
        Guid agentId,
        string playerCode,
        DateOnly from,
        DateOnly to,
        decimal deposit)
    {
        var fixture = new Fixture();
        fixture.Customize<AgentPlayerDepositModel>(u => u
            .With(s => s.BrandId, brandId)
            .With(s => s.AgentId, agentId)
            .With(s => s.PlayerCode, playerCode)
            .With(s => s.From, from)
            .With(s => s.To, to)
            .With(s => s.DepositAmount, deposit));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }

    private static Fixture CreateAgentContractConditionFixture(int rank,
        Guid agentDividendContractId,
        int validNumber,
        decimal validBetAmount,
        decimal netProfit,
        decimal percentage)
    {
        var fixture = new Fixture();
        fixture.Customize<AgentDividendContractPercentageModel>(u => u
            .With(s => s.Rank, rank)
            .With(s => s.WinLossAmount, validBetAmount)
            .With(s => s.ValidNumber, validNumber)
            .With(s => s.NetProfit, netProfit)
            .With(s => s.Percentage, percentage)
            .With(s => s.AgentDividendContractId, agentDividendContractId));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }
}