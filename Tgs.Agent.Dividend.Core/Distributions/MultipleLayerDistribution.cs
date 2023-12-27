using System.Runtime.CompilerServices;
using Tgs.Agent.Dividend.Domain;
using Tgs.Agent.Dividend.Domain.Interfaces;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.Core.Distributions;

internal class MultipleLayerDistribution(IContractRepository contractRepository, IRepository repository)
    : IDistribution
{
    public async IAsyncEnumerable<AgentPlayerAggregateModel> GetAgentPlayerAggregateAsync(int idNumber,
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

        agentAggregate.Id = idNumber + 1;

        yield return agentAggregate;
    }

    public async IAsyncEnumerable<AgentDividendMultipleLayerModel> GenerateMultipleLayerAsync(BrandModel brand,
        AgentDividendContractModel contract,
        AgentPlayerAggregateModel agentPlayerAggregate,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var betPlayers = await repository
            .GetAgentPlayerBetCountsCacheAsync(brand.Id,
                brand.CurrentSettlementFrom,
                brand.CurrentSettlementTo,
                cancellationToken)
            .Where(x => x.AgentId == contract.AgentId)
            .ToArrayAsync(cancellationToken);

        var playerCodes = betPlayers.Select(x => x.PlayerCode);

        var conditions = await contractRepository
            .GetAgentDividendContractConditionsAsync(contract.Id, cancellationToken)
            .ToArrayAsync(cancellationToken);

        var validPlayerCount = GetValidPlayerCount(playerCodes, betPlayers, contract);

        var agentDividend = GetAgentDividendsByMultipleLayer(agentPlayerAggregate.Id,
            contract.AgentId,
            contract.Id,
            brand.CheckDividendActiveCount,
            new AgentConditionModel
            {
                ValidNumber = validPlayerCount,
                WinLossAmount = agentPlayerAggregate.WinLossAmount,
                NetProfit = agentPlayerAggregate.NetProfit(brand.PlatformCharge)
            },
            conditions);

        agentPlayerAggregate.AgentDividendPercentage = agentDividend.AgentDividendPercentage;

        yield return agentDividend;
    }

    public IEnumerable<AgentPlayerAggregateModel> GenerateAdditionalAgentPlayerAggregates(int idNumber,
        BrandModel brand,
        AgentDividendMultipleLayerModel multipleLayer,
        IEnumerable<AgentPlayerAggregateModel> currentAllAgentPlayerAggregates,
        CancellationToken cancellationToken)
    {
        return Enumerable.Empty<AgentPlayerAggregateModel>();
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
        Guid contractId,
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
            ContractId = contractId,
            DividendProviderAgentId = agentId,
            DividendProviderId = id,
            AgentDividendPercentage = agentDividendPercentage,
            ActualDividendPercentage = agentDividendPercentage,
            AgentCoveredBonus = (agentCurrentCondition.BonusAmount * agentDividendPercentage / 100M).Truncate(2),
            AgentCoveredBonusAdjust =
                (agentCurrentCondition.BonusAdjustAmount * agentDividendPercentage / 100M).Truncate(2),
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
                x => currentCondition.NetProfit >= x.NetProfit && currentCondition.WinLossAmount >= x.WinLossAmount)
            .Where(checkDividendActiveCount,
                x => currentCondition.ValidNumber >= x.ValidNumber &&
                     currentCondition.NetProfit >= x.NetProfit &&
                     currentCondition.WinLossAmount >= x.WinLossAmount)
            .Select(x => x.Percentage)
            .FirstOrDefault();
    }

    private static int GetValidPlayerCount(
        IEnumerable<string> playerCodes,
        IEnumerable<AgentPlayerBetDaysModel> betDays,
        AgentDividendContractModel contract)
    {
        return playerCodes.Where(playerCode =>
            {
                var betDay = betDays.FirstOrDefault(x => x.PlayerCode == playerCode)?.DayCount ?? 0;

                return betDay >= contract.BetDays;
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