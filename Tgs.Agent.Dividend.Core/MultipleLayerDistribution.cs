using System.Runtime.CompilerServices;
using Tgs.Agent.Dividend.Domain;
using Tgs.Agent.Dividend.Domain.Interfaces;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.Core;

internal class MultipleLayerDistribution(IContractRepository contractRepository, IRepository repository)
    : IDistribution
{
    public async IAsyncEnumerable<(AgentPlayerAggregateModel, AgentDividendMultipleLayerModel[])>
        AgentDividendGenerationAsync(int idNumber,
            BrandModel brand,
            AgentDividendContractModel contract,
            [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var agentAggregate = await repository
            .GetAgentPlayerAggregatesForMultipleLayerCacheAsync(brand.Id,
                brand.CurrentSettlementFrom,
                brand.CurrentSettlementTo,
                cancellationToken)
            .Where(x => x.AgentId == contract.AgentId)
            .FirstOrDefaultAsync(cancellationToken);

        var agentPlayerGameAggregate =
            await repository.GetAgentPlayerGameAggregatesCacheAsync(brand.Id,
                    brand.CurrentSettlementFrom,
                    brand.CurrentSettlementTo)
                .FirstOrDefaultAsync(x => x.AgentId == contract.AgentId, cancellationToken);

        agentAggregate ??= GetDefaultAggregate(brand, contract.AgentId);

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

        agentAggregate.Id = idNumber;

        var from = new DateTimeOffset(brand.CurrentSettlementFrom.Year,
            brand.CurrentSettlementFrom.Month,
            brand.CurrentSettlementFrom.Day,
            0,
            0,
            0,
            TimeSpan.Zero).ToOffset(TimeSpan.FromHours(8));

        var to = new DateTimeOffset(brand.CurrentSettlementTo.Year,
            brand.CurrentSettlementTo.Month,
            brand.CurrentSettlementTo.Day,
            0,
            0,
            0,
            TimeSpan.Zero).ToOffset(TimeSpan.FromHours(8));

        var validBets = await repository
            .GetAgentPlayerValidBetsCacheAsync(brand.Id,
                brand.CurrentSettlementFrom,
                brand.CurrentSettlementTo,
                cancellationToken)
            .Where(x => x.AgentId == contract.AgentId)
            .ToArrayAsync(cancellationToken);

        var betDays = await repository
            .GetAgentPlayerBetCountsCacheAsync(brand.Id,
                brand.CurrentSettlementFrom,
                brand.CurrentSettlementTo,
                cancellationToken)
            .Where(x => x.AgentId == contract.AgentId)
            .ToArrayAsync(cancellationToken);

        var deposits = await repository
            .GetAgentPlayerDepositsCacheAsync(brand.Id,
                DateOnly.FromDateTime(from.DateTime),
                DateOnly.FromDateTime(to.DateTime),
                cancellationToken)
            .Where(x => x.AgentId == contract.AgentId)
            .ToArrayAsync(cancellationToken);

        var playerCodes = validBets.Select(x => x.PlayerCode)
            .Concat(betDays.Select(x => x.PlayerCode))
            .Concat(deposits.Select(x => x.PlayerCode))
            .Distinct();

        var conditions = await contractRepository
            .GetAgentDividendContractConditionsAsync(contract.Id, cancellationToken)
            .ToArrayAsync(cancellationToken);

        var validPlayerCount = GetValidPlayerCount(playerCodes, deposits, validBets, betDays, contract);

        var agentDividend = GetAgentDividendsByMultipleLayer(agentAggregate.Id,
            contract.AgentId,
            brand.CheckDividendActiveCount,
            new AgentConditionModel
            {
                ValidNumber = validPlayerCount, 
                ValidBetAmount = validBets.Sum(s => s.ValidBetAmount),
                NetProfit = agentAggregate.NetProfit(brand.PlatformCharge)
            },
            conditions);

        agentAggregate.AgentDividendPercentage = agentDividend.AgentDividendPercentage;

        yield return (agentAggregate, new[] { agentDividend });
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

    private static AgentDividendMultipleLayerModel GetAgentDividendsByMultipleLayer(
        int id,
        Guid agentId,
        bool checkDividendActiveCount,
        AgentConditionModel agentCurrentCondition,
        IEnumerable<AgentDividendContractPercentageModel> agentDividendConditions)
    {
        var layer = agentId.ToString();

        var agentDividendPercentage = GetAgentDividendPercentage(agentDividendConditions,
            agentCurrentCondition,
            checkDividendActiveCount);

        return new AgentDividendMultipleLayerModel
        {
            AgentId = agentId,
            DividendProviderAgentId = agentId,
            DividendProviderId = id,
            AgentDividendPercentage = agentDividendPercentage,
            ActualDividendPercentage = agentDividendPercentage,
            Layer = layer,
            Dividend = (agentCurrentCondition.NetProfit * agentDividendPercentage / 100M).Truncate(2)
        };
    }

    private static decimal GetAgentDividendPercentage(
        IEnumerable<AgentDividendContractPercentageModel> agentDividendCondition,
        AgentConditionModel currentCondition,
        bool checkDividendActiveCount)
    {
        return agentDividendCondition.OrderByDescending(x => x.Rank)
            .Where(!checkDividendActiveCount,
                x => currentCondition.NetProfit >= x.NetProfit && currentCondition.ValidBetAmount >= x.WinLossAmount)
            .Where(checkDividendActiveCount,
                x => currentCondition.ValidNumber >= x.ValidNumber &&
                     currentCondition.NetProfit >= x.NetProfit &&
                     currentCondition.ValidBetAmount >= x.WinLossAmount)
            .Select(x => x.Percentage)
            .FirstOrDefault();
    }

    private static int GetValidPlayerCount(
        IEnumerable<string> playerCodes,
        IEnumerable<AgentPlayerDepositModel> deposits,
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

    private static AgentPlayerAggregateModel GetDefaultAggregate(BrandModel brand, Guid agentId)
    {
        return new AgentPlayerAggregateModel
        {
            BrandId = brand.Id,
            AgentId = agentId,
            From = brand.CurrentSettlementFrom,
            To = brand.CurrentSettlementTo
        };
    }
}