using System.Text.Json;
using Tgs.Agent.Dividend.Domain;
using Tgs.Agent.Dividend.Domain.Interfaces;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.Core;

internal class OneLayerDistribution(IContractRepository contractRepository, IRepository repository) : IDistribution
{
    public IAsyncEnumerable<(AgentPlayerAggregateModel, AgentDividendMultipleLayerModel[])>
        AgentDividendGenerationAsync(int idNumber,
            BrandModel brand,
            AgentDividendContractModel contract,
            CancellationToken cancellationToken)
    {
        var agentAggregates = repository
            .GetAgentPlayerAggregatesCacheAsync(brand.Id,
                brand.CurrentSettlementFrom,
                brand.CurrentSettlementTo,
                cancellationToken)
            .Where(x => x.AgentId == contract.AgentId);

        return agentAggregates.Select((x, i) =>
        {
            var agentDividendPercentage =
                JsonSerializer.Deserialize<AgentDividendPercentageData[]>(x.AgentDividendPercentageTreeData!);

            var bonusAdjustAmount = x.BonusAdjustAmount - x.NoIncludeNetProfitBonusAdjustAmount;

            x.Id = idNumber + i;

            return (x, GetAgentDividends(
                    x.Id,
                    contract.Id,
                    agentDividendPercentage!,
                    x.AgentId,
                    x.BonusAmount,
                    bonusAdjustAmount,
                    x.NetProfit(brand.PlatformCharge))
                .ToArray());
        });
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
        Guid contractId,
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
                ContractId = contractId,
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
}