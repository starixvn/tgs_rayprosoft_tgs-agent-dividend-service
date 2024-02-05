using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tgs.Agent.Dividend.Domain;
using Tgs.Agent.Dividend.Domain.Interfaces;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.Core.Distributions;

internal class OneLayerDividendCalculator(IContractRepository contractRepository, IRepository repository)
    : IDividendCalculator
{
    public async IAsyncEnumerable<AgentPlayerAggregateModel> GetAgentPlayerAggregateAsync(int idNumber,
        AgentModel agent,
        AgentDividendContractModel contract,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var agentAggregates = repository
            .GetAgentPlayerAggregatesCacheAsync(agent.Id,
                agent.CurrentSettlementFrom,
                agent.CurrentSettlementTo,
                cancellationToken)
            .Where(x => x.AgentId == agent.Id);

        var betPlayers = await repository
            .GetAgentPlayerBetCountsCacheAsync(agent.Id,
                agent.CurrentSettlementFrom,
                agent.CurrentSettlementTo,
                cancellationToken)
            .Where(x => x.AgentId == agent.Id)
            .ToArrayAsync(cancellationToken);
        var i = 0;

        var agents = await repository.GetAgentsCacheAsyncEnumerable(agent.Id, cancellationToken)
            .ToArrayAsync(cancellationToken);

        if (!await agentAggregates.AnyAsync(cancellationToken))
            yield return GetDefaultAggregate(idNumber + 1, agent, agents);
        else
            await foreach (var agentPlayerAggregateModel in agentAggregates)
            {
                agentPlayerAggregateModel.Id = idNumber + i + 1;
                agentPlayerAggregateModel.ActiveNumber = betPlayers.Length;

                i++;

                yield return agentPlayerAggregateModel;
            }
    }

    public async IAsyncEnumerable<AgentDividendMultipleLayerModel> GenerateMultipleLayerAsync(
        BrandModel brand,
        AgentDividendContractModel contract,
        AgentPlayerAggregateModel agentPlayerAggregate,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var agentDividendPercentage =
            JsonSerializer.Deserialize<AgentDividendPercentageData[]>(agentPlayerAggregate
                .AgentDividendPercentageTreeData!);

        var snapshotId =
            await GenerateAgentDividendSnapshotAsync(contract, agentDividendPercentage!, cancellationToken);

        var bonusAdjustAmount = agentPlayerAggregate.BonusAdjustAmount -
                                agentPlayerAggregate.NoIncludeNetProfitBonusAdjustAmount;

        foreach (var agentDividendMultipleLayerModel in GetAgentDividends(
                     agentPlayerAggregate.Id,
                     snapshotId,
                     agentDividendPercentage!,
                     agentPlayerAggregate.AgentId,
                     agentPlayerAggregate.BonusAmount,
                     bonusAdjustAmount,
                     agentPlayerAggregate.NetProfit(brand.PlatformCharge,
                         contract.DepositFeePercentage,
                         contract.WithdrawFeePercentage,
                         contract.SportsbookPercentage,
                         contract.VirtualSportsPercentage,
                         contract.LiveDealerPercentage,
                         contract.CardGamePercentage,
                         contract.LotteryPercentage,
                         contract.SlotPercentage)))
            yield return agentDividendMultipleLayerModel;
    }

    public decimal GetAgentDividendAmount(AgentPlayerAggregateModel agentPlayerAggregate,
        IEnumerable<AgentDividendMultipleLayerModel> multipleLayer,
        CancellationToken cancellation)
    {
        return multipleLayer.Where(p
                       => p.AgentId == agentPlayerAggregate.AgentId &&
                          p.AgentDividendPercentage == agentPlayerAggregate.AgentDividendPercentage)
                   .Sum(s => s.Dividend) -
               (agentPlayerAggregate.AgentAdjustAmount - agentPlayerAggregate.NoIncludeNetProfitAgentAdjustAmount);
    }

    public IEnumerable<AgentDividendMultipleLayerModel> GetMultipleLayer(AgentPlayerAggregateModel agentPlayerAggregate,
        IEnumerable<AgentDividendMultipleLayerModel> multipleLayer,
        CancellationToken cancellation)
    {
        return multipleLayer.Where(p
            => p.AgentId == agentPlayerAggregate.AgentId &&
               p.AgentDividendPercentage == agentPlayerAggregate.AgentDividendPercentage);
    }

    private static IEnumerable<AgentDividendMultipleLayerModel> GetAgentDividends(
        int id,
        Guid snapshotId,
        AgentDividendPercentageData[] agentDividendPercentages,
        Guid agentId,
        decimal bonusAmount,
        decimal bonusAdjustAmount,
        decimal netProfit)
    {
        bool hasParentAgentId;

        var agent = agentDividendPercentages.First(x => x.AgentId == agentId);
        var currentAgentId = agentId;
        var providerAgentId = agent.AgentId;
        var childDividendPercentage = 0M;
        var layer = agent.AgentId.ToString();

        do
        {
            var agentDividendPercentage = agent.DividendPercentage;
            var actualDividendPercentage = agentDividendPercentage - childDividendPercentage;

            yield return new AgentDividendMultipleLayerModel
            {
                AgentDividendSnapshotId = snapshotId,
                AgentId = currentAgentId,
                DividendProviderAgentId = providerAgentId,
                DividendProviderId = id,
                AgentDividendPercentage = agentDividendPercentage,
                ActualDividendPercentage = actualDividendPercentage,
                Layer = layer,
                AgentCoveredBonus = (bonusAmount * actualDividendPercentage / 100M).Truncate(2),
                AgentCoveredBonusAdjust = (bonusAdjustAmount * actualDividendPercentage / 100M).Truncate(2),
                Dividend = (netProfit * actualDividendPercentage / 100M).Truncate(2)
            };

            hasParentAgentId = agent.ParentAgentId != null;

            if (!hasParentAgentId) continue;

            childDividendPercentage = agentDividendPercentage;

            var parentAgent = agentDividendPercentages.First(x => x.AgentId == agent.ParentAgentId);
            currentAgentId = parentAgent.AgentId;

            layer = $"{currentAgentId},{layer}";

            agent = parentAgent;
        } while (hasParentAgentId);
    }

    private static AgentPlayerAggregateModel GetDefaultAggregate(int id,
        AgentModel agent,
        AgentModel[] agents)
    {
        return new AgentPlayerAggregateModel
        {
            Id = id,
            BrandId = agent.BrandId,
            AgentId = agent.Id,
            AgentDividendPercentage = agent.DividendPercentage,
            From = agent.CurrentSettlementFrom,
            To = agent.CurrentSettlementTo,
            AgentDividendPercentageTreeData = GenerateAgentDividendPercentages(agent.Path, agents)
        };
    }

    private async Task<Guid> GenerateAgentDividendSnapshotAsync(AgentDividendContractModel contract,
        IEnumerable<AgentDividendPercentageData> conditions,
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

    private static string? GenerateAgentDividendPercentages(LTree? agentPath, AgentModel[] agents)
    {
        if (agentPath is null)
        {
            return null;
        }

        var agentIds = agentPath.ToString()!.Split(".").Select(Guid.Parse);
        var agentTree = agents.Where(x => agentIds.Contains(x.Id)).Select(x => new AgentDividendPercentageData
        {
            AgentId = x.Id,
            ParentAgentId = x.ParentAgentId,
            DividendPercentage = x.DividendPercentage
        });
        return JsonSerializer.Serialize(agentTree);
    }
}