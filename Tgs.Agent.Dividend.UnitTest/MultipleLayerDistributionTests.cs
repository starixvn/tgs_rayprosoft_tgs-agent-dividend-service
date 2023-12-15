using AutoFixture;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Tgs.Agent.Dividend.Core;
using Tgs.Agent.Dividend.Domain.Enums;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.UnitTest;

public class MultipleLayerDistributionTests
{
    [SetUp]
    public void Setup()
    {
    }

    [TestCase(-110, 0, 0, 0, 1, 2, 0)]
    [TestCase(-100, 0, 0, 0, 1, 2, 0)]
    [TestCase(-100, 2, 2, 2, 1, 2, 0)]
    [TestCase(110, 0, 0, 0, 1, 2, 13.2)]
    [TestCase(100, 0, 0, 0, 1, 2, 10.0)]
    [TestCase(100, 2, 2, 2, 1, 2, 0)]
    [TestCase(110, 0, 0, 0, 1, 2, 13.2)]
    [TestCase(100, 0, 0, 0, 1, 2, 10.0)]
    [TestCase(100, 2, 2, 2, 1, 2, 0)]
    public async Task Generate_Agent_Dividend(decimal winLossAmount,
        decimal contractValidBetAmount,
        decimal contractDepositAmount,
        int contractBetDays,
        int player1BetDays,
        int player2BetDays,
        decimal dividend)
    {
        var fakeContractRepository = Substitute.For<IContractRepository>();
        var fakeRepository = Substitute.For<IRepository>();

        var sut = new MultipleLayerDistribution(
            fakeContractRepository,
            fakeRepository);

        var brandId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var agentId2 = Guid.NewGuid();
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

        var brand = CreateBrandFixture(brandId, from, to, 0, AgentDividendType.OneLayer).Create<BrandModel>();

        var agents = new[]
        {
            CreateAgentFixture(brandId, agentId, agentId2, new LTree($"{agentId2:N}.{agentId:N}"))
                .Create<AgentModel>(),
            CreateAgentFixture(brandId, agentId2, null, new LTree($"{agentId2:N}")).Create<AgentModel>()
        };

        var agentPlayerAggregates = new[]
        {
            CreateAgentPlayerAggregateFixture(brandId, agentId, from, to, winLossAmount)
                .Create<AgentPlayerAggregateModel>()
        };

        var agentPlayerGameAggregates = new[]
        {
            CreateAgentPlayerGameAggregateFixture(brandId, agentId, from, to)
                .Create<AgentPlayerGameAggregateModel>()
        };

        var playerBetDays = new[]
        {
            CreateAgentPlayerBetCountFixture(brandId, agentId, player1, from, to, player1BetDays)
                .Create<AgentPlayerBetDaysModel>(),
            CreateAgentPlayerBetCountFixture(brandId, agentId, player2, from, to, player2BetDays)
                .Create<AgentPlayerBetDaysModel>()
        };

        var conditions = new[]
        {
            CreateAgentContractConditionFixture(1, contract.Id, 2, 0, 100, 10)
                .Create<AgentDividendContractPercentageModel>(),
            CreateAgentContractConditionFixture(2, contract.Id, 2, 0, 110, 12)
                .Create<AgentDividendContractPercentageModel>()
        };

        fakeRepository.GetBrandAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(brand);
        fakeRepository.GetMaxAgentDividendIdAsync(Arg.Any<CancellationToken>()).Returns(99);
        fakeRepository.GetAgentsCacheAsyncEnumerable(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(agents.ToAsyncEnumerable());

        fakeRepository
            .GetAgentPlayerAggregatesForMultipleLayerCacheAsync(brandId, from, to, Arg.Any<CancellationToken>())
            .Returns(agentPlayerAggregates.ToAsyncEnumerable());

        fakeRepository
            .GetAgentPlayerGameAggregatesCacheAsync(brandId, from, to)
            .Returns(agentPlayerGameAggregates.ToAsyncEnumerable());

        fakeRepository.GetAgentPlayerBetCountsCacheAsync(brandId, from, to, Arg.Any<CancellationToken>())
            .Returns(playerBetDays.ToAsyncEnumerable());

        fakeContractRepository.GetAgentDividendContractConditionsAsync(contract.Id, Arg.Any<CancellationToken>())
            .Returns(conditions.ToAsyncEnumerable());

        var response = await sut.GetAgentPlayerAggregateAsync(10, brand, contract, default).ToArrayAsync();

        Assert.That(response, Has.Length.EqualTo(1));
    }

    [TestCase(-110, 0, 0, 0, 1, 2, 0)]
    [TestCase(-100, 0, 0, 0, 1, 2, 0)]
    [TestCase(-100, 2, 2, 2, 1, 2, 0)]
    [TestCase(110, 0, 0, 0, 1, 2, 13.2)]
    [TestCase(100, 0, 0, 0, 1, 2, 10.0)]
    [TestCase(100, 2, 2, 2, 1, 2, 0)]
    [TestCase(110, 0, 0, 0, 1, 2, 13.2)]
    [TestCase(100, 0, 0, 0, 1, 2, 10.0)]
    [TestCase(100, 2, 2, 2, 1, 2, 0)]
    public async Task Generate_Agent_Dividend2(decimal winLossAmount,
    decimal contractValidBetAmount,
    decimal contractDepositAmount,
    int contractBetDays,
    int player1BetDays,
    int player2BetDays,
    decimal dividend)
    {
        var fakeContractRepository = Substitute.For<IContractRepository>();
        var fakeRepository = Substitute.For<IRepository>();

        var sut = new MultipleLayerDistribution(
            fakeContractRepository,
            fakeRepository);

        var brandId = Guid.NewGuid();
        var agentId = Guid.NewGuid();
        var agentId2 = Guid.NewGuid();
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

        var brand = CreateBrandFixture(brandId, from, to, 0, AgentDividendType.OneLayer).Create<BrandModel>();

        var agents = new[]
        {
            CreateAgentFixture(brandId, agentId, agentId2, new LTree($"{agentId2:N}.{agentId:N}"))
                .Create<AgentModel>(),
            CreateAgentFixture(brandId, agentId2, null, new LTree($"{agentId2:N}")).Create<AgentModel>()
        };

        var agentPlayerAggregate = CreateAgentPlayerAggregateFixture(brandId, agentId, from, to, winLossAmount)
            .Create<AgentPlayerAggregateModel>();

        var agentPlayerGameAggregates = new[]
        {
            CreateAgentPlayerGameAggregateFixture(brandId, agentId, from, to)
                .Create<AgentPlayerGameAggregateModel>()
        };

        var playerBetDays = new[]
        {
            CreateAgentPlayerBetCountFixture(brandId, agentId, player1, from, to, player1BetDays)
                .Create<AgentPlayerBetDaysModel>(),
            CreateAgentPlayerBetCountFixture(brandId, agentId, player2, from, to, player2BetDays)
                .Create<AgentPlayerBetDaysModel>()
        };

        var conditions = new[]
        {
            CreateAgentContractConditionFixture(1, contract.Id, 2, 0, 100, 10)
                .Create<AgentDividendContractPercentageModel>(),
            CreateAgentContractConditionFixture(2, contract.Id, 2, 0, 110, 12)
                .Create<AgentDividendContractPercentageModel>()
        };

        fakeRepository.GetBrandAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(brand);
        fakeRepository.GetMaxAgentDividendIdAsync(Arg.Any<CancellationToken>()).Returns(99);
        fakeRepository.GetAgentsCacheAsyncEnumerable(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(agents.ToAsyncEnumerable());

        fakeRepository
            .GetAgentPlayerAggregatesForMultipleLayerCacheAsync(brandId, from, to, Arg.Any<CancellationToken>())
            .Returns(new[]{ agentPlayerAggregate }.ToAsyncEnumerable());

        fakeRepository
            .GetAgentPlayerGameAggregatesCacheAsync(brandId, from, to)
            .Returns(agentPlayerGameAggregates.ToAsyncEnumerable());

        fakeRepository.GetAgentPlayerBetCountsCacheAsync(brandId, from, to, Arg.Any<CancellationToken>())
            .Returns(playerBetDays.ToAsyncEnumerable());

        fakeContractRepository.GetAgentDividendContractConditionsAsync(contract.Id, Arg.Any<CancellationToken>())
            .Returns(conditions.ToAsyncEnumerable());

        var response = await sut.GenerateMultipleLayerAsync(brand, contract, agentPlayerAggregate, default).ToArrayAsync();

        Assert.That(response, Has.Length.EqualTo(1));

        Assert.That(response.First().Dividend, Is.EqualTo(dividend));
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
        decimal winLoss)
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
            .With(s => s.NoIncludeNetProfitBonusAdjustAmount, 0));
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