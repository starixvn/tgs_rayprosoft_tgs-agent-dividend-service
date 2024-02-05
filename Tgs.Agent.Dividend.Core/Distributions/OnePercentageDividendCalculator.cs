using System.Runtime.CompilerServices;
using System.Text.Json;
using Tgs.Agent.Dividend.Domain;
using Tgs.Agent.Dividend.Domain.Interfaces;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.Core.Distributions;

internal class OnePercentageDividendCalculator(IContractRepository contractRepository, IRepository repository)
    : IDividendCalculator
{
    public async IAsyncEnumerable<AgentPlayerAggregateModel> GetAgentPlayerAggregateAsync(int idNumber,
        AgentModel agent,
        AgentDividendContractModel contract,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var agentAggregate = await repository
            .GetAgentPlayerAggregatesForMultipleLayerCacheAsync(agent.BrandId,
                agent.CurrentSettlementFrom,
                agent.CurrentSettlementTo,
                cancellationToken)
            .Where(x => x.AgentId == agent.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var agentPlayerGameAggregate =
            await repository.GetAgentPlayerGameAggregatesCacheAsync(agent.BrandId,
                    agent.CurrentSettlementFrom,
                    agent.CurrentSettlementTo)
                .FirstOrDefaultAsync(x => x.AgentId == agent.Id, cancellationToken);

        var from = new DateTimeOffset(agent.CurrentSettlementFrom.Year,
            agent.CurrentSettlementFrom.Month,
            agent.CurrentSettlementFrom.Day,
            0,
            0,
            0,
            TimeSpan.Zero).ToOffset(TimeSpan.FromHours(8));

        var to = new DateTimeOffset(agent.CurrentSettlementTo.Year,
            agent.CurrentSettlementTo.Month,
            agent.CurrentSettlementTo.Day,
            0,
            0,
            0,
            TimeSpan.Zero).ToOffset(TimeSpan.FromHours(8));

        var validBets = await repository
            .GetAgentPlayerValidBetsCacheAsync(agent.BrandId,
                agent.CurrentSettlementFrom,
                agent.CurrentSettlementTo,
                cancellationToken)
            .Where(x => x.AgentId == agent.Id)
            .ToArrayAsync(cancellationToken);

        var betDays = await repository
            .GetAgentPlayerBetCountsCacheAsync(agent.BrandId,
                agent.CurrentSettlementFrom,
                agent.CurrentSettlementTo,
                cancellationToken)
            .Where(x => x.AgentId == agent.Id)
            .ToArrayAsync(cancellationToken);

        var paymentAmounts = await repository
            .GetAgentPlayerPaymentsCacheAsync(agent.BrandId,
                DateOnly.FromDateTime(from.DateTime),
                DateOnly.FromDateTime(to.DateTime),
                cancellationToken)
            .Where(x => x.AgentId == agent.Id)
            .ToArrayAsync(cancellationToken);

        var playerCodes = validBets.Select(x => x.PlayerCode)
            .Concat(betDays.Select(x => x.PlayerCode))
            .Concat(paymentAmounts.Select(x => x.PlayerCode))
            .Distinct();

        var validPlayerCount = GetValidPlayerCount(playerCodes, paymentAmounts, validBets, betDays, contract);

        agentAggregate ??= GetDefaultAggregate(agent);

        agentAggregate.DepositAmount = paymentAmounts.Sum(x => x.DepositAmount);
        agentAggregate.WithdrawalAmount = paymentAmounts.Sum(x => x.WithdrawAmount);
        agentAggregate.LiveDealerWinLoss = agentPlayerGameAggregate?.LiveDealerWinLoss ?? 0M;
        agentAggregate.SlotWinLoss = agentPlayerGameAggregate?.SlotWinLoss ?? 0M;
        agentAggregate.FishingWinLoss = agentPlayerGameAggregate?.FishingWinLoss ?? 0M;
        agentAggregate.ArcadeWinLoss = agentPlayerGameAggregate?.ArcadeWinLoss ?? 0M;
        agentAggregate.TableGameWinLoss = agentPlayerGameAggregate?.TableGameWinLoss ?? 0M;
        agentAggregate.LotteryWinLoss = agentPlayerGameAggregate?.LotteryWinLoss ?? 0M;
        agentAggregate.CardGameWinLoss = agentPlayerGameAggregate?.CardGameWinLoss ?? 0M;
        agentAggregate.SportsWinLoss = agentPlayerGameAggregate?.SportsWinLoss ?? 0M;
        agentAggregate.ESportsWinLoss = agentPlayerGameAggregate?.ESportsWinLoss ?? 0M;
        agentAggregate.VirtualSportsWinLoss = agentPlayerGameAggregate?.VirtualSportsWinLoss ?? 0M;
        agentAggregate.ActiveNumber = validPlayerCount;
        agentAggregate.Id = idNumber + 1;

        yield return agentAggregate;
    }

    public async IAsyncEnumerable<AgentDividendMultipleLayerModel> GenerateMultipleLayerAsync(BrandModel brand,
        AgentDividendContractModel contract,
        AgentPlayerAggregateModel agentPlayerAggregate,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var conditions = await contractRepository
            .GetAgentDividendContractConditionsAsync(contract.Id, cancellationToken)
            .ToArrayAsync(cancellationToken);

        var snapshotId = await GenerateAgentDividendSnapshotAsync(contract, conditions, cancellationToken);

        var agent = await repository.GetAgentsCacheAsyncEnumerable(brand.Id, cancellationToken)
            .FirstAsync(x => x.Id == contract.AgentId, cancellationToken);

        var agents = await repository
            .GetDescendantAgentsByPathCacheAsyncEnumerable(brand.Id, agent.Path, cancellationToken)
            .ToArrayAsync(cancellationToken);

        var agentDividends = GetAgentDividendsByMultipleLayer(
            agentPlayerAggregate.Id,
            snapshotId,
            agent,
            contract,
            brand.CheckDividendActiveCount,
            agents,
            new AgentConditionModel
            {
                ValidNumber = agentPlayerAggregate.ActiveNumber,
                BonusAmount = agentPlayerAggregate.BonusAmount,
                BonusAdjustAmount = agentPlayerAggregate.BonusAdjustAmount,
                WinLossAmount = agentPlayerAggregate.WinLossAmount,
                NetProfit = agentPlayerAggregate.NetProfit(brand.PlatformCharge,
                    contract.DepositFeePercentage,
                    contract.WithdrawFeePercentage,
                    contract.SportsbookPercentage,
                    contract.VirtualSportsPercentage,
                    contract.LiveDealerPercentage,
                    contract.CardGamePercentage,
                    contract.LotteryPercentage,
                    contract.SlotPercentage)
            },
            conditions);

        foreach (var agentDividendMultipleLayerModel in agentDividends) yield return agentDividendMultipleLayerModel;
    }

    public decimal GetAgentDividendAmount(AgentPlayerAggregateModel agentPlayerAggregate,
        IEnumerable<AgentDividendMultipleLayerModel> multipleLayer,
        CancellationToken cancellation)
    {
        return multipleLayer.Where(p => p.AgentId == agentPlayerAggregate.AgentId)
                   .Sum(s => s.Dividend) -
               (agentPlayerAggregate.AgentAdjustAmount - agentPlayerAggregate.NoIncludeNetProfitAgentAdjustAmount);
    }

    public IEnumerable<AgentDividendMultipleLayerModel> GetMultipleLayer(AgentPlayerAggregateModel agentPlayerAggregate,
        IEnumerable<AgentDividendMultipleLayerModel> multipleLayer,
        CancellationToken cancellation)
    {
        return multipleLayer.Where(p => p.AgentId == agentPlayerAggregate.AgentId);
    }

    private static int GetValidPlayerCount(
        IEnumerable<string> playerCodes,
        IEnumerable<AgentPlayerPaymentModel> deposits,
        IEnumerable<AgentPlayerValidBetModel> validBets,
        IEnumerable<AgentPlayerBetDaysModel> betDays,
        AgentDividendContractModel contract)
    {
        return playerCodes.Where(playerCode =>
            {
                var deposit = deposits.FirstOrDefault(x => x.PlayerCode == playerCode)?.DepositAmount ??
                              0M;
                var validBet = validBets.FirstOrDefault(x => x.PlayerCode == playerCode)?.ValidBetAmount ??
                               0M;
                var betDay = betDays.FirstOrDefault(x => x.PlayerCode == playerCode)?.DayCount ?? 0;

                return deposit >= contract.DepositAmount &&
                       validBet >= contract.ValidBetAmount &&
                       betDay >= contract.BetDays;
            })
            .Count();
    }

    private static IEnumerable<AgentDividendMultipleLayerModel> GetAgentDividendsByMultipleLayer(
        int id,
        Guid snapshotId,
        AgentModel agent,
        AgentDividendContractModel contract,
        bool checkDividendActiveCount,
        IEnumerable<AgentModel> lineAgents,
        AgentConditionModel agentCurrentCondition,
        IEnumerable<AgentDividendContractConditionModel> agentDividendConditions)
    {
        var (meetingTheRequirement, percentage) = GetAgentDividendPercentage(agentDividendConditions,
            agentCurrentCondition,
            checkDividendActiveCount);

        if (!meetingTheRequirement && agentCurrentCondition.NetProfit > 0)
            return lineAgents.OrderByDescending(x => x.Path).Select(lineAgent =>
            {
                var path = lineAgent.Path.ToString();

                var pathList = path.Split(".");

                var layer = pathList.Aggregate((result, x) => $"{result},{Guid.Parse(x)}");

                return new AgentDividendMultipleLayerModel
                {
                    AgentId = lineAgent.Id,
                    AgentDividendSnapshotId = snapshotId,
                    DividendProviderAgentId = agent.Id,
                    DividendProviderId = id,
                    AgentDividendPercentage = 0M,
                    ActualDividendPercentage = 0M,
                    AgentCoveredBonus = 0,
                    AgentCoveredBonusAdjust = 0,
                    Layer = layer,
                    Dividend = 0M
                };
            });

        return lineAgents.OrderByDescending(x => x.Path).Select((lineAgent, i) =>
        {
            var path = lineAgent.Path.ToString();

            var pathList = path.Split(".");

            var layer = pathList.Aggregate((result, x) => $"{result},{Guid.Parse(x)}");

            if (agent.Id == lineAgent.Id)
                return new AgentDividendMultipleLayerModel
                {
                    AgentId = lineAgent.Id,
                    AgentDividendSnapshotId = snapshotId,
                    DividendProviderAgentId = agent.Id,
                    DividendProviderId = id,
                    AgentDividendPercentage = percentage,
                    ActualDividendPercentage = percentage,
                    AgentCoveredBonus = (agentCurrentCondition.BonusAmount * percentage / 100M).Truncate(2),
                    AgentCoveredBonusAdjust = (agentCurrentCondition.BonusAdjustAmount * percentage / 100M).Truncate(2),
                    Layer = layer,
                    Dividend = (agentCurrentCondition.NetProfit * percentage / 100M).Truncate(2)
                };

            var levelPercentage = contract.LevelPercentages.GetValue(i);

            return new AgentDividendMultipleLayerModel
            {
                AgentId = lineAgent.Id,
                AgentDividendSnapshotId = snapshotId,
                DividendProviderAgentId = agent.Id,
                DividendProviderId = id,
                AgentDividendPercentage = (decimal?)levelPercentage ?? 0M,
                ActualDividendPercentage = (decimal?)levelPercentage ?? 0M,
                AgentCoveredBonus = (agentCurrentCondition.BonusAmount * (decimal?)levelPercentage ?? 0M / 100).Truncate(2),
                AgentCoveredBonusAdjust = (agentCurrentCondition.BonusAdjustAmount * (decimal?)levelPercentage ?? 0M / 100).Truncate(2),
                Layer = layer,
                Dividend = (agentCurrentCondition.NetProfit * (decimal?)levelPercentage ?? 0M / 100).Truncate(2)
            };
        });
    }

    private static (bool meetingTheRequirement, decimal percentage) GetAgentDividendPercentage(
        IEnumerable<AgentDividendContractConditionModel> agentDividendCondition,
        AgentConditionModel currentCondition,
        bool checkDividendActiveCount)
    {
        var condition = agentDividendCondition.OrderByDescending(x => x.Rank)
            .Where(!checkDividendActiveCount,
                x => Math.Abs(currentCondition.WinLossAmount) >= x.WinLossAmount)
            .Where(checkDividendActiveCount,
                x => currentCondition.ValidNumber >= x.ValidNumber &&
                     Math.Abs(currentCondition.WinLossAmount) >= x.WinLossAmount)
            .FirstOrDefault();

        return (condition != null, condition?.Percentage ?? 0M);
    }

    private static AgentPlayerAggregateModel GetDefaultAggregate(AgentModel agent)
    {
        return new AgentPlayerAggregateModel
        {
            BrandId = agent.BrandId,
            AgentId = agent.Id,
            From = agent.CurrentSettlementFrom,
            To = agent.CurrentSettlementTo
        };
    }

    private async Task<Guid> GenerateAgentDividendSnapshotAsync(AgentDividendContractModel contract,
        IEnumerable<AgentDividendContractConditionModel> conditions,
        CancellationToken cancellationToken)
    {
        var snapshotId = Guid.NewGuid();

        var data = JsonSerializer.Serialize(new
        {
            Contract = contract,
            Conditions = conditions
        });

        await contractRepository.CreateAgentContractSnapshotAsync(snapshotId, data, cancellationToken);

        return snapshotId;
    }
}