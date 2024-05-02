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
        BrandModel brand,
        AgentModel agent,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var agentAggregates = repository
            .GetAgentPlayerAggregatesCacheAsync(brand.Id,
                brand.CurrentSettlementFrom,
                brand.CurrentSettlementTo,
                cancellationToken)
            .Where(x => x.AgentId == agent.Id);

        var betPlayers = await repository
            .GetAgentPlayerBetCountsCacheAsync(brand.Id,
                brand.CurrentSettlementFrom,
                brand.CurrentSettlementTo,
                cancellationToken)
            .Where(x => x.AgentId == agent.Id)
            .ToArrayAsync(cancellationToken);

        var i = 0;

        if (!await agentAggregates.AnyAsync(cancellationToken))
        {
            var agents = await repository.GetAgentsCacheAsyncEnumerable(brand.Id, cancellationToken)
                .ToArrayAsync(cancellationToken);

            yield return GetDefaultAggregate(idNumber + 1, agent.Id, brand, agents);
        }
        else
            await foreach (var agentPlayerAggregateModel in agentAggregates)
            {
                agentPlayerAggregateModel.Id = idNumber + i + 1;
                agentPlayerAggregateModel.ActiveNumber = betPlayers.Length;

                agentPlayerAggregateModel.AgentDividendPlatformCharges = await repository
                    .GetAgentPlatformChargeCacheAsync(brand.Id,
                        agentPlayerAggregateModel.AgentDividendPercentage,
                        brand.CurrentSettlementFrom,
                        brand.CurrentSettlementTo,
                        cancellationToken)
                    .Where(x => x.AgentId == agent.Id)
                    .ToArrayAsync(cancellationToken);

                foreach (var agentDividendPlatformChargeModel in agentPlayerAggregateModel.AgentDividendPlatformCharges)
                {
                    agentDividendPlatformChargeModel.AgentDividendId = agentPlayerAggregateModel.Id;
                }

                agentPlayerAggregateModel.AgentDividendProductFees = await repository
                    .GetAgentProductFeeCacheAsync(brand.Id,
                        agentPlayerAggregateModel.AgentDividendPercentage,
                        brand.CurrentSettlementFrom,
                        brand.CurrentSettlementTo,
                        cancellationToken)
                    .Where(x => x.AgentId == agent.Id)
                    .ToArrayAsync(cancellationToken);

                foreach (var agentDividendProductFeeModel in agentPlayerAggregateModel.AgentDividendProductFees)
                {
                    agentDividendProductFeeModel.AgentDividendId = agentPlayerAggregateModel.Id;
                }

                i++;

                yield return agentPlayerAggregateModel;
            }
    }

    public async IAsyncEnumerable<AgentDividendMultipleLayerModel> GenerateMultipleLayerAsync(BrandModel brand,
        AgentModel agent,
        AgentPlayerAggregateModel agentPlayerAggregate,
        bool preCalculation,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var agentDividendPercentage =
            JsonSerializer.Deserialize<AgentDividendPercentageData[]>(agentPlayerAggregate
                .AgentDividendPercentageTreeData!);

        var snapshotId = preCalculation
            ? Guid.Empty
            : await GenerateAgentDividendSnapshotAsync(agent, agentDividendPercentage!, cancellationToken);

        var bonusAdjustAmount = agentPlayerAggregate.BonusAdjustAmount -
                                agentPlayerAggregate.NoIncludeNetProfitBonusAdjustAmount;

        foreach (var agentDividendMultipleLayerModel in GetAgentDividends(
                     agentPlayerAggregate.Id,
                     snapshotId,
                     agentDividendPercentage!,
                     agentPlayerAggregate.AgentId,
                     agentPlayerAggregate.BonusAmount,
                     bonusAdjustAmount,
                     agentPlayerAggregate.NetProfit()))
            yield return agentDividendMultipleLayerModel;
    }

    public decimal GetAgentDividendAmount(AgentPlayerAggregateModel agentPlayerAggregate,
        IEnumerable<AgentDividendMultipleLayerModel> multipleLayer,
        CancellationToken cancellation)
    {
        return multipleLayer.Where(p
                       => (p.AgentId == agentPlayerAggregate.AgentId &&
                           p.AgentDividendPercentage == agentPlayerAggregate.AgentDividendPercentage &&
                           p.AgentId != p.DividendProviderAgentId) ||
                          (p.AgentId == agentPlayerAggregate.AgentId &&
                           p.AgentDividendPercentage == agentPlayerAggregate.AgentDividendPercentage &&
                           p.AgentId == p.DividendProviderAgentId &&
                           agentPlayerAggregate.Id == p.DividendProviderId))
                   .Sum(s => s.Dividend) -
               (agentPlayerAggregate.AgentAdjustAmount - agentPlayerAggregate.NoIncludeNetProfitAgentAdjustAmount);
    }

    public IEnumerable<AgentDividendMultipleLayerModel> GetMultipleLayer(AgentPlayerAggregateModel agentPlayerAggregate,
        IEnumerable<AgentDividendMultipleLayerModel> multipleLayer,
        CancellationToken cancellation)
    {
        return multipleLayer.Where(p
            => (p.AgentId == agentPlayerAggregate.AgentId &&
                p.AgentDividendPercentage == agentPlayerAggregate.AgentDividendPercentage &&
                p.AgentId != p.DividendProviderAgentId) ||
               (p.AgentId == agentPlayerAggregate.AgentId &&
                p.AgentDividendPercentage == agentPlayerAggregate.AgentDividendPercentage &&
                p.AgentId == p.DividendProviderAgentId &&
                agentPlayerAggregate.Id == p.DividendProviderId));
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
            var agentDividendPercentage = agent.DividendPercentage < 0 ? 0 : agent.DividendPercentage;
            var actualDividendPercentage = agentDividendPercentage - (childDividendPercentage < 0 ? 0 : childDividendPercentage);

            yield return new AgentDividendMultipleLayerModel
            {
                AgentId = currentAgentId,
                AgentDividendSnapshotId = snapshotId,
                DividendProviderAgentId = providerAgentId,
                DividendProviderId = id,
                AgentDividendPercentage = agent.DividendPercentage,
                ActualDividendPercentage = actualDividendPercentage,
                Layer = layer,
                AgentCoveredBonus = (bonusAmount * actualDividendPercentage / 100M).Truncate(2),
                AgentCoveredBonusAdjust = (bonusAdjustAmount * actualDividendPercentage / 100M).Truncate(2),
                Dividend = (netProfit * actualDividendPercentage / 100M).Truncate(2)
            };

            hasParentAgentId = agent.ParentAgentId != null;

            if (!hasParentAgentId) continue;

            childDividendPercentage = agent.DividendPercentage;

            var parentAgent = agentDividendPercentages.First(x => x.AgentId == agent.ParentAgentId);
            currentAgentId = parentAgent.AgentId;

            layer = $"{currentAgentId},{layer}";

            agent = parentAgent;
        } while (hasParentAgentId);
    }

    private static AgentPlayerAggregateModel GetDefaultAggregate(int id,
        Guid agentId,
        BrandModel brand,
        AgentModel[] agents)
    {
        var agent = agents.First(x => x.Id == agentId);

        return new AgentPlayerAggregateModel
        {
            Id = id,
            BrandId = brand.Id,
            AgentId = agent.Id,
            AgentDividendPercentage = agent.DividendPercentage,
            From = brand.CurrentSettlementFrom,
            To = brand.CurrentSettlementTo,
            AgentDividendPercentageTreeData = GenerateAgentDividendPercentages(agent.Path, agents)
        };
    }

    private async Task<Guid> GenerateAgentDividendSnapshotAsync(AgentModel agent,
        IEnumerable<AgentDividendPercentageData> conditions,
        CancellationToken cancellationToken)
    {
        var snapshotId = Guid.NewGuid();

        var data = JsonSerializer.Serialize(new
        {
            AgentId = agent.Id,
            agent.BrandId,
            agent.NegativeAgentDividendCarryForward,
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