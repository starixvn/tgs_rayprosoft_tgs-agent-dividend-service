using System.Runtime.CompilerServices;
using System.Text.Json;
using Tgs.Agent.Dividend.Domain;
using Tgs.Agent.Dividend.Domain.Interfaces;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.Core.Distributions;

internal class MultipleLayerDividendCalculator(IContractRepository contractRepository, IRepository repository)
    : IDividendCalculator
{
    public async IAsyncEnumerable<AgentPlayerAggregateModel> GetAgentPlayerAggregateAsync(int idNumber,
        BrandModel brand,
        AgentModel agent,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var agentAggregate = await repository
            .GetAgentPlayerAggregatesForMultipleLayerCacheAsync(brand.Id,
                brand.CurrentSettlementFrom,
                brand.CurrentSettlementTo,
                cancellationToken,
                false)
            .Where(x => x.AgentId == agent.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var agentPlayerGameAggregate =
            await repository.GetAgentPlayerGameAggregatesCacheAsync(brand.Id,
                    brand.CurrentSettlementFrom,
                    brand.CurrentSettlementTo,
                    cancellationToken)
                .FirstOrDefaultAsync(x => x.AgentId == agent.Id, cancellationToken);

        var validPlayerCount = await repository
            .GetAgentPlayerBetCountsCacheAsync(brand.Id,
                brand.CurrentSettlementFrom,
                brand.CurrentSettlementTo,
                cancellationToken)
            .Where(x => x.AgentId == agent.Id)
            .GroupBy(x => x.PlayerCode)
            .CountAsync(cancellationToken);

        agentAggregate ??= GetDefaultAggregate(brand, agent.Id);

        agentAggregate.Id = idNumber + 1;

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

        agentAggregate.AgentDividendPlatformCharges = await repository
            .GetAgentPlatformChargeCacheAsync(brand.Id,
                agentAggregate.AgentDividendPercentage,
                brand.CurrentSettlementFrom,
                brand.CurrentSettlementTo,
                cancellationToken)
            .Where(x => x.AgentId == agent.Id)
            .ToArrayAsync(cancellationToken);

        foreach (var agentAggregateAgentDividendPlatformCharge in agentAggregate.AgentDividendPlatformCharges)
        {
            agentAggregateAgentDividendPlatformCharge.AgentDividendId = agentAggregate.Id;
        }

        agentAggregate.AgentDividendProductFees = await repository
            .GetAgentProductFeeCacheAsync(brand.Id,
                agentAggregate.AgentDividendPercentage,
                brand.CurrentSettlementFrom,
                brand.CurrentSettlementTo,
                cancellationToken)
            .Where(x => x.AgentId == agent.Id)
            .ToArrayAsync(cancellationToken);

        foreach (var agentAggregateAgentDividendProductFee in agentAggregate.AgentDividendProductFees)
        {
            agentAggregateAgentDividendProductFee.AgentDividendId = agentAggregate.Id;
        }

        yield return agentAggregate;
    }

    public async IAsyncEnumerable<AgentDividendMultipleLayerModel> GenerateMultipleLayerAsync(BrandModel brand,
        AgentModel agent,
        AgentPlayerAggregateModel agentPlayerAggregate,
        bool preCalculation,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var conditions = await contractRepository
            .GetAgentDividendConditionsAsync(agent.Id, cancellationToken)
            .ToArrayAsync(cancellationToken);

        var snapshotId = preCalculation ? Guid.Empty : await GenerateAgentDividendSnapshotAsync(agent, conditions, cancellationToken);

        var agentDividend = GetAgentDividendsByMultipleLayer(agentPlayerAggregate.Id,
            agent.Id,
            snapshotId,
            brand.CheckDividendActiveCount,
            new AgentConditionModel
            {
                ValidNumber = agentPlayerAggregate.ActiveNumber,
                WinLossAmount = agentPlayerAggregate.WinLossAmount,
                NetProfit = agentPlayerAggregate.NetProfit()
            },
            conditions);

        agentPlayerAggregate.AgentDividendPercentage = agentDividend.AgentDividendPercentage;

        yield return agentDividend;
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
        Guid snapshotId,
        bool checkDividendActiveCount,
        AgentConditionModel agentCurrentCondition,
        IEnumerable<AgentDividendConditionModel> agentDividendConditions)
    {
        var layer = agentId.ToString();

        var agentDividendPercentage = GetAgentDividendPercentage(agentDividendConditions,
            agentCurrentCondition,
            checkDividendActiveCount);

        return new AgentDividendMultipleLayerModel
        {
            AgentId = agentId,
            AgentDividendSnapshotId = snapshotId,
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
        IEnumerable<AgentDividendConditionModel> agentDividendCondition,
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

    private async Task<Guid> GenerateAgentDividendSnapshotAsync(AgentModel agent,
        IEnumerable<AgentDividendConditionModel> conditions,
        CancellationToken cancellationToken)
    {
        var snapshotId = Guid.NewGuid();

        var data = JsonSerializer.Serialize(new
        {
            AgentId = agent.Id,
            agent.BrandId,
            Conditions = conditions
        });

        await contractRepository.CreateAgentContractSnapshotAsync(snapshotId, data, cancellationToken);

        return snapshotId;
    }
}