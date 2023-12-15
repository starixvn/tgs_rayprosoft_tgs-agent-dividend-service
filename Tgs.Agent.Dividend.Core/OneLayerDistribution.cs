using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Tgs.Agent.Dividend.Domain;
using Tgs.Agent.Dividend.Domain.Interfaces;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.Core;

internal class OneLayerDistribution(IRepository repository) : IDistribution
{
    public IAsyncEnumerable<AgentPlayerAggregateModel> GetAgentPlayerAggregateAsync(int idNumber,
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
            x.Id = idNumber + i + 1;

            return x;
        });
    }

    public IAsyncEnumerable<AgentDividendMultipleLayerModel> GenerateMultipleLayerAsync(
        BrandModel brand,
        AgentDividendContractModel contract,
        AgentPlayerAggregateModel agentPlayerAggregate,
        CancellationToken cancellationToken)
    {
        var agentDividendPercentage =
            JsonSerializer.Deserialize<AgentDividendPercentageData[]>(agentPlayerAggregate.AgentDividendPercentageTreeData!);

        var bonusAdjustAmount = agentPlayerAggregate.BonusAdjustAmount - agentPlayerAggregate.NoIncludeNetProfitBonusAdjustAmount;

        return GetAgentDividends(
            agentPlayerAggregate.Id,
            contract.Id,
            agentDividendPercentage!,
            agentPlayerAggregate.AgentId,
            agentPlayerAggregate.BonusAmount,
            bonusAdjustAmount,
            agentPlayerAggregate.NetProfit(brand.PlatformCharge)).ToAsyncEnumerable();
    }

    public async IAsyncEnumerable<AgentPlayerAggregateModel> GenerateAdditionalAgentPlayerAsync(int idNumber,
        BrandModel brand,
        AgentDividendMultipleLayerModel layer,
        IAsyncEnumerable<AgentPlayerAggregateModel> currentAllAgentPlayerAggregates,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var exists = await currentAllAgentPlayerAggregates.AnyAsync(a =>
            a.AgentId == layer.AgentId && a.AgentDividendPercentage == layer.AgentDividendPercentage,
            cancellationToken: cancellationToken);

        if (exists) yield break;

        var model = GetDefaultAggregate(idNumber + 1, brand, layer.AgentId, layer.AgentDividendPercentage);

        yield return model;
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

    private static AgentPlayerAggregateModel GetDefaultAggregate(int id, BrandModel brand, Guid agentId, decimal agentDividendPercentage)
    {
        return new AgentPlayerAggregateModel
        {
            Id = id,
            BrandId = brand.Id,
            AgentId = agentId,
            AgentDividendPercentage = agentDividendPercentage,
            From = brand.CurrentSettlementFrom,
            To = brand.CurrentSettlementTo
        };
    }
}