using AutoFixture;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tgs.Agent.Dividend.Core.Distributions;
using Tgs.Agent.Dividend.Domain.Enums;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.UnitTest;

public class OnePercentageDistributionTests
{
    [SetUp]
    public void Setup()
    {
    }

    [TestCase(-100, 20, 30, 30, -26.4)]
    [TestCase(-100, 20, 30, 0, -33)]
    public async Task Generate_Agent_Dividend_With_Bonus(decimal winLossAmount,
        decimal bonusAmount,
        decimal bonusAdjustAmount,
        decimal noIncludeNetProfitBonusAdjustAmount,
        decimal totalDividend)
    {
        var fakeContractRepository = Substitute.For<IContractRepository>();
        var fakeRepository = Substitute.For<IRepository>();

        var brandId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var agentId2 = Guid.NewGuid();
        var agentId3 = Guid.NewGuid();
        var agentId4 = Guid.NewGuid();
        var from = DateTime.Parse("2023/12/06 00:00:00+8");
        var to = DateTime.Parse("2023/12/06 00:00:00+8");
        var player1 = "player1";
        var player2 = "player2";

        var contract = CreateContractFixture(brandId,
                agentId,
                ContractDividendType.OnePercentage,
                2,
                2,
                2)
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

        var agentPlayerAggregates = new[]
        {
            CreateAgentPlayerAggregateFixture(brandId,
                    agentId,
                    from,
                    to,
                    winLossAmount,
                    2,
                    bonusAmount,
                    bonusAdjustAmount,
                    noIncludeNetProfitBonusAdjustAmount)
                .Create<AgentPlayerAggregateModel>()
        };

        var agentPlayerGameAggregates = new[]
        {
            CreateAgentPlayerGameAggregateFixture(brandId, agentId, from, to)
                .Create<AgentPlayerGameAggregateModel>()
        };

        var playerValidBets = new[]
        {
            CreateAgentPlayerValidBetFixture(brandId, agentId, player1, from, to, 5)
                .Create<AgentPlayerValidBetModel>(),
            CreateAgentPlayerValidBetFixture(brandId, agentId, player2, from, to, 1)
                .Create<AgentPlayerValidBetModel>()
        };

        var playerBetDays = new[]
        {
            CreateAgentPlayerBetCountFixture(brandId, agentId, player1, from, to, 2)
                .Create<AgentPlayerBetDaysModel>(),
            CreateAgentPlayerBetCountFixture(brandId, agentId, player2, from, to, 2)
                .Create<AgentPlayerBetDaysModel>()
        };

        var playerDeposits = new[]
        {
            CreateAgentPlayerDepositFixture(brandId,
                    agentId,
                    player1,
                    DateOnly.FromDateTime(from),
                    DateOnly.FromDateTime(to),
                    2)
                .Create<AgentPlayerDepositModel>(),
            CreateAgentPlayerDepositFixture(brandId,
                    agentId,
                    player2,
                    DateOnly.FromDateTime(from),
                    DateOnly.FromDateTime(to),
                    1)
                .Create<AgentPlayerDepositModel>()
        };

        var conditions = new[]
        {
            CreateAgentContractConditionFixture(1, contract.Id, 2, 10, 0, 5)
                .Create<AgentDividendContractConditionModel>(),
            CreateAgentContractConditionFixture(2, contract.Id, 2, 100, 0, 10)
                .Create<AgentDividendContractConditionModel>(),
            CreateAgentContractConditionFixture(3, contract.Id, 2, 110, 0, 12)
                .Create<AgentDividendContractConditionModel>()
        };

        fakeRepository.GetBrandAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(brand);
        fakeRepository.GetAgentsCacheAsyncEnumerable(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(agents.ToAsyncEnumerable());
        fakeRepository
            .GetDescendantAgentsByPathCacheAsyncEnumerable(Arg.Any<Guid>(),
                Arg.Any<LTree>(),
                Arg.Any<CancellationToken>())
            .Returns(agents.ToAsyncEnumerable());

        fakeRepository
            .GetAgentPlayerAggregatesForMultipleLayerCacheAsync(brandId, from, to, Arg.Any<CancellationToken>())
            .Returns(agentPlayerAggregates.ToAsyncEnumerable());

        fakeRepository
            .GetAgentPlayerGameAggregatesCacheAsync(brandId, from, to)
            .Returns(agentPlayerGameAggregates.ToAsyncEnumerable());

        fakeRepository.GetAgentPlayerValidBetsCacheAsync(brandId, from, to, Arg.Any<CancellationToken>())
            .Returns(playerValidBets.ToAsyncEnumerable());

        fakeRepository.GetAgentPlayerBetCountsCacheAsync(brandId, from, to, Arg.Any<CancellationToken>())
            .Returns(playerBetDays.ToAsyncEnumerable());

        fakeRepository.GetAgentPlayerDepositsCacheAsync(brandId,
                DateOnly.FromDateTime(from),
                DateOnly.FromDateTime(to),
                Arg.Any<CancellationToken>())
            .Returns(playerDeposits.ToAsyncEnumerable());

        fakeContractRepository.GetAgentDividendContractConditionsAsync(contract.Id, Arg.Any<CancellationToken>())
            .Returns(conditions.ToAsyncEnumerable());

        var multipleLayers = new List<AgentDividendMultipleLayerModel>();
        foreach (var agentPlayerAggregateModel in agentPlayerAggregates)
        {
            var sut = new OnePercentageDividendCalculator(
                fakeContractRepository,
                fakeRepository);

            multipleLayers.AddRange(await sut
                .GenerateMultipleLayerAsync(brand, contract, agentPlayerAggregateModel, default)
                .ToArrayAsync());
        }

        Assert.That(multipleLayers, Has.Count.EqualTo(4));

        Assert.That(multipleLayers.Sum(x => x.Dividend), Is.EqualTo(totalDividend));
    }

    [TestCase(-110, 1, 0, 0,2, -26.4)]
    [TestCase(-100, 1, 0, 0, 2, -22.0)]
    [TestCase(-50, 1, 1, 1, 2, -8.5)]
    [TestCase(-100, 2, 2, 2, 2, -22.0)]
    [TestCase(110, 1, 0, 0, 2, 26.4)]
    [TestCase(100, 1, 0, 0, 2, 22.0)]
    [TestCase(50, 1, 1, 1, 2, 8.5)]
    [TestCase(100, 2, 2, 2, 1, 0)]
    public async Task Generate_Agent_Dividend_For_One_Percentage_Type(decimal winLossAmount,
        decimal contractValidBetAmount,
        decimal contractDepositAmount,
        int contractBetDays,
        int activeNumber,
        decimal totalDividend)
    {
        var fakeContractRepository = Substitute.For<IContractRepository>();
        var fakeRepository = Substitute.For<IRepository>();

        var brandId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var agentId2 = Guid.NewGuid();
        var agentId3 = Guid.NewGuid();
        var agentId4 = Guid.NewGuid();
        var from = DateTime.Parse("2023/12/06 00:00:00+8");
        var to = DateTime.Parse("2023/12/06 00:00:00+8");
        var player1 = "player1";
        var player2 = "player2";

        var contract = CreateContractFixture(brandId,
                agentId,
                ContractDividendType.MultipleLayer,
                contractValidBetAmount,
                contractDepositAmount,
                contractBetDays)
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

        var agentPlayerAggregates = new[]
        {
            CreateAgentPlayerAggregateFixture(brandId, agentId, from, to, winLossAmount, activeNumber)
                .Create<AgentPlayerAggregateModel>()
        };

        var agentPlayerGameAggregates = new[]
        {
            CreateAgentPlayerGameAggregateFixture(brandId, agentId, from, to)
                .Create<AgentPlayerGameAggregateModel>()
        };

        var conditions = new[]
        {
            CreateAgentContractConditionFixture(1, contract.Id, 2, 10, 0, 5)
                .Create<AgentDividendContractConditionModel>(),
            CreateAgentContractConditionFixture(2, contract.Id, 2, 100, 0, 10)
                .Create<AgentDividendContractConditionModel>(),
            CreateAgentContractConditionFixture(3, contract.Id, 2, 110, 0, 12)
                .Create<AgentDividendContractConditionModel>()
        };

        fakeRepository.GetBrandAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(brand);
        fakeRepository.GetMaxAgentDividendIdAsync(Arg.Any<CancellationToken>()).Returns(99);
        fakeRepository.GetAgentsCacheAsyncEnumerable(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(agents.ToAsyncEnumerable());
        fakeRepository
            .GetDescendantAgentsByPathCacheAsyncEnumerable(Arg.Any<Guid>(),
                Arg.Any<LTree>(),
                Arg.Any<CancellationToken>())
            .Returns(agents.ToAsyncEnumerable());

        fakeRepository
            .GetAgentPlayerAggregatesForMultipleLayerCacheAsync(brandId, from, to, Arg.Any<CancellationToken>())
            .Returns(agentPlayerAggregates.ToAsyncEnumerable());

        fakeRepository
            .GetAgentPlayerGameAggregatesCacheAsync(brandId, from, to)
            .Returns(agentPlayerGameAggregates.ToAsyncEnumerable());

        fakeContractRepository.GetAgentDividendContractConditionsAsync(contract.Id, Arg.Any<CancellationToken>())
            .Returns(conditions.ToAsyncEnumerable());

        var multipleLayers = new List<AgentDividendMultipleLayerModel>();
        foreach (var agentPlayerAggregateModel in agentPlayerAggregates)
        {
            var sut = new OnePercentageDividendCalculator(
                fakeContractRepository,
                fakeRepository);

            multipleLayers.AddRange(await sut
                .GenerateMultipleLayerAsync(brand, contract, agentPlayerAggregateModel, default)
                .ToArrayAsync());
        }

        Assert.That(multipleLayers, Has.Count.EqualTo(4));

        Assert.That(multipleLayers.Sum(x => x.Dividend), Is.EqualTo(totalDividend));
    }

    [TestCase(100, 0, 0, 0, 20, 1, 1, 6, 2, 2, 0)]
    [TestCase(100, 0, 0, 0, 4, 1, 1, 6, 2, 2, 0)]
    [TestCase(100, 2, 2, 2, 1, 1, 1, 4, 2, 2, 0)]
    public async Task Generate_Agent_Dividend_With_No_Round_Aggregate(decimal winLossAmount,
        decimal contractValidBetAmount,
        decimal contractDepositAmount,
        int contractBetDays,
        decimal player1ValidBetAmount,
        decimal player1DepositAmount,
        int player1BetDays,
        decimal player2ValidBetAmount,
        decimal player2DepositAmount,
        int player2BetDays,
        decimal totalDividend)
    {
        var fakeContractRepository = Substitute.For<IContractRepository>();
        var fakeRepository = Substitute.For<IRepository>();

        var brandId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var agentId2 = Guid.NewGuid();
        var agentId3 = Guid.NewGuid();
        var agentId4 = Guid.NewGuid();
        var from = DateTime.Parse("2023/12/06 00:00:00+8");
        var to = DateTime.Parse("2023/12/06 00:00:00+8");
        var player1 = "player1";
        var player2 = "player2";

        var contract = CreateContractFixture(brandId,
                agentId,
                ContractDividendType.MultipleLayer,
                contractValidBetAmount,
                contractDepositAmount,
                contractBetDays)
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

        var agentPlayerAggregates = new AgentPlayerAggregateModel[]
        {
        };

        var agentPlayerGameAggregates = new AgentPlayerGameAggregateModel[]
        {
        };

        var playerValidBets = new[]
        {
            CreateAgentPlayerValidBetFixture(brandId, agentId, player1, from, to, player1ValidBetAmount)
                .Create<AgentPlayerValidBetModel>(),
            CreateAgentPlayerValidBetFixture(brandId, agentId, player2, from, to, player2ValidBetAmount)
                .Create<AgentPlayerValidBetModel>()
        };

        var playerBetDays = new[]
        {
            CreateAgentPlayerBetCountFixture(brandId, agentId, player1, from, to, player1BetDays)
                .Create<AgentPlayerBetDaysModel>(),
            CreateAgentPlayerBetCountFixture(brandId, agentId, player2, from, to, player2BetDays)
                .Create<AgentPlayerBetDaysModel>()
        };

        var playerDeposits = new[]
        {
            CreateAgentPlayerDepositFixture(brandId,
                    agentId,
                    player1,
                    DateOnly.FromDateTime(from),
                    DateOnly.FromDateTime(to),
                    player1DepositAmount)
                .Create<AgentPlayerDepositModel>(),
            CreateAgentPlayerDepositFixture(brandId,
                    agentId,
                    player2,
                    DateOnly.FromDateTime(from),
                    DateOnly.FromDateTime(to),
                    player2DepositAmount)
                .Create<AgentPlayerDepositModel>()
        };

        var conditions = new[]
        {
            CreateAgentContractConditionFixture(1, contract.Id, 2, 10, 10, 10)
                .Create<AgentDividendContractConditionModel>(),
            CreateAgentContractConditionFixture(2, contract.Id, 2, 11, 11, 12)
                .Create<AgentDividendContractConditionModel>()
        };

        fakeRepository.GetBrandAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(brand);
        fakeRepository.GetMaxAgentDividendIdAsync(Arg.Any<CancellationToken>()).Returns(99);
        fakeRepository.GetAgentsCacheAsyncEnumerable(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(agents.ToAsyncEnumerable());
        fakeRepository
            .GetDescendantAgentsByPathCacheAsyncEnumerable(Arg.Any<Guid>(),
                Arg.Any<LTree>(),
                Arg.Any<CancellationToken>())
            .Returns(agents.ToAsyncEnumerable());

        fakeRepository
            .GetAgentPlayerAggregatesForMultipleLayerCacheAsync(brandId, from, to, Arg.Any<CancellationToken>())
            .Returns(agentPlayerAggregates.ToAsyncEnumerable());

        fakeRepository
            .GetAgentPlayerGameAggregatesCacheAsync(brandId, from, to)
            .Returns(agentPlayerGameAggregates.ToAsyncEnumerable());

        fakeRepository.GetAgentPlayerValidBetsCacheAsync(brandId, from, to, Arg.Any<CancellationToken>())
            .Returns(playerValidBets.ToAsyncEnumerable());

        fakeRepository.GetAgentPlayerBetCountsCacheAsync(brandId, from, to, Arg.Any<CancellationToken>())
            .Returns(playerBetDays.ToAsyncEnumerable());

        fakeRepository.GetAgentPlayerDepositsCacheAsync(brandId,
                DateOnly.FromDateTime(from),
                DateOnly.FromDateTime(to),
                Arg.Any<CancellationToken>())
            .Returns(playerDeposits.ToAsyncEnumerable());

        fakeContractRepository.GetAgentDividendContractConditionsAsync(contract.Id, Arg.Any<CancellationToken>())
            .Returns(conditions.ToAsyncEnumerable());

        var multipleLayers = new List<AgentDividendMultipleLayerModel>();
        foreach (var agentPlayerAggregateModel in agentPlayerAggregates)
        {
            var sut = new OnePercentageDividendCalculator(
                fakeContractRepository,
                fakeRepository);

            multipleLayers.AddRange(await sut
                .GenerateMultipleLayerAsync(brand, contract, agentPlayerAggregateModel, default)
                .ToArrayAsync());
        }

        Assert.That(multipleLayers, Has.Count.EqualTo(0));

        Assert.That(multipleLayers.Sum(x => x.Dividend), Is.EqualTo(totalDividend));
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
        int activeNumber,
        decimal? bonusAmount = 0,
        decimal? bonusAdjustAmount = 0,
        decimal? noIncludeNetProfitBonusAdjustAmount = 0)
    {
        var fixture = new Fixture();
        fixture.Customize<AgentPlayerAggregateModel>(u => u
            .With(s => s.BrandId, brandId)
            .With(s => s.AgentId, agentId)
            .With(s => s.From, from)
            .With(s => s.To, to)
            .With(s => s.WinLossAmount, winLoss)
            .With(s => s.ActiveNumber, activeNumber)
            .With(s => s.BonusAmount, bonusAmount)
            .With(s => s.BonusAdjustAmount, bonusAdjustAmount)
            .With(s => s.NoIncludeNetProfitBonusAdjustAmount, noIncludeNetProfitBonusAdjustAmount));
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
        decimal winLossAmount,
        decimal netProfit,
        decimal percentage)
    {
        var fixture = new Fixture();
        fixture.Customize<AgentDividendContractConditionModel>(u => u
            .With(s => s.Rank, rank)
            .With(s => s.WinLossAmount, winLossAmount)
            .With(s => s.ValidNumber, validNumber)
            .With(s => s.NetProfit, netProfit)
            .With(s => s.Percentage, percentage)
            .With(s => s.AgentDividendContractId, agentDividendContractId));
        fixture.Behaviors.Add(new OmitOnRecursionBehavior());
        return fixture;
    }
}