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
        AgentDividendContractModel contract,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var agentAggregate = await repository
            .GetAgentPlayerAggregatesForMultipleLayerCacheAsync(brand.Id,
                brand.CurrentSettlementFrom,
                brand.CurrentSettlementTo,
                cancellationToken,
                false)
            .Where(x => x.AgentId == contract.AgentId)
            .FirstOrDefaultAsync(cancellationToken);

        var agentPlayerGameAggregate =
            await repository.GetAgentPlayerGameAggregatesCacheAsync(brand.Id,
                    brand.CurrentSettlementFrom,
                    brand.CurrentSettlementTo)
                .FirstOrDefaultAsync(x => x.AgentId == contract.AgentId, cancellationToken);

        var betPlayers = await repository
            .GetAgentPlayerBetCountsCacheAsync(brand.Id,
                brand.CurrentSettlementFrom,
                brand.CurrentSettlementTo,
                cancellationToken)
            .Where(x => x.AgentId == contract.AgentId)
            .ToArrayAsync(cancellationToken);

        var playerCodes = betPlayers.Select(x => x.PlayerCode);

        var validPlayerCount = GetValidPlayerCount(playerCodes, betPlayers, contract);

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

        var agentDividend = GetAgentDividendsByMultipleLayer(agentPlayerAggregate.Id,
            contract.AgentId,
            snapshotId,
            brand.CheckDividendActiveCount,
            new AgentConditionModel
            {
                ValidNumber = agentPlayerAggregate.ActiveNumber,
                WinLossAmount = agentPlayerAggregate.WinLossAmount,
                NetProfit = agentPlayerAggregate.NetProfit(brand.PlatformCharge)
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
        IEnumerable<AgentDividendContractConditionModel> agentDividendConditions)
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
        IEnumerable<AgentDividendContractConditionModel> agentDividendCondition,
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

    private async Task<Guid> GenerateAgentDividendSnapshotAsync(AgentDividendContractModel contract,
        IEnumerable<AgentDividendContractConditionModel> conditions,
        CancellationToken cancellationToken)
    {
        var snapshotId = Guid.NewGuid();

        var data = JsonSerializer.Serialize(new
        {
            contract.Id,
            contract.BrandId,
            contract.AgentId,
            contract.Name,
            contract.ContractDividendType,
            contract.ContractStatus,
            contract.ValidBetAmount,
            contract.DepositAmount,
            contract.BetDays,
            Conditions = conditions
        });

        await contractRepository.CreateAgentContractSnapshotAsync(snapshotId, data, cancellationToken);

        return snapshotId;
    }
}