using System.Runtime.CompilerServices;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Tgs.Agent.Client;
using Tgs.Agent.Client.Models;
using Tgs.Agent.Dividend.Domain.Enums;
using Tgs.Agent.Dividend.Domain.Interfaces;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.Core;

internal class GetAgentDividendBeforehandHandler(
    IMediator mediator,
    IRepository repository,
    IAgentClient agentClient,
    IServiceProvider serviceProvider)
    : IRequestHandler<GetAgentDividendBeforehandRequest, AgentDividendBeforehandResponse>
{
    public async Task<AgentDividendBeforehandResponse> Handle(GetAgentDividendBeforehandRequest request,
        CancellationToken cancellationToken)
    {
        var brand = await repository.GetBrandAsync(request.BrandId, cancellationToken).ConfigureAwait(false);

        var previous = await repository
            .GetAgentDividendBeforehandAsync(request.AgentId, brand.PreviousSettlementFrom, brand.PreviousSettlementTo)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var lastAgentDividend = previous.MaxBy(x => x.Id);

        var lastNegativeDividendAmount = Math.Min(0M, lastAgentDividend?.TotalDividendAmount ?? 0M);

        return new AgentDividendBeforehandResponse
        {
            Previous = previous,
            Current = await GetCurrentAgentDividendBeforehandAsync(brand, request.AgentId, lastNegativeDividendAmount,
                    cancellationToken)
                .ToArrayAsync(cancellationToken).ConfigureAwait(false)
        };
    }

    private async IAsyncEnumerable<AgentDividendBeforehand> GetCurrentAgentDividendBeforehandAsync(BrandModel brand,
        Guid agentId,
        decimal negativeAgentDividendAmount,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var childAgents = await agentClient
            .GetChildAgentInfosAsync(new GetChildAgentInfosRequest(agentId), cancellationToken)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var agentIds = childAgents.Select(x => x.AgentId).Append(agentId).ToArray();

        var agents = await repository.GetAgentsCacheAsyncEnumerable(brand.Id, cancellationToken)
            .Where(x => agentIds.Contains(x.Id))
            .ToArrayAsync(cancellationToken);

        var agentInstances = agents
            .Select(x =>
            {
                var distributionService =
                    serviceProvider.GetRequiredKeyedService<IDividendCalculator>(
                        ((AgentDividendType)brand.AgentDividendType).ToString());

                return (agent: x, distributionService);
            })
            .ToArray();

        var agentPlayerAggregates = new List<AgentPlayerAggregateModel>();

        var currentNumber = 0;

        foreach (var instance in agentInstances)
        {
            var data = await instance.distributionService.GetAgentPlayerAggregateAsync(currentNumber,
                    brand,
                    instance.agent,
                    cancellationToken)
                .ToArrayAsync(cancellationToken);

            currentNumber += data.Length;

            agentPlayerAggregates.AddRange(data);
        }

        var multipleLayers = new List<AgentDividendMultipleLayerModel>();

        foreach (var agentPlayerAggregateModel in agentPlayerAggregates)
        {
            var (agent, distributionService) =
                agentInstances.First(a => a.agent.Id == agentPlayerAggregateModel.AgentId);

            var data = await distributionService.GenerateMultipleLayerAsync(
                    brand,
                    agent,
                    agentPlayerAggregateModel,
                    true,
                    cancellationToken)
                .ToArrayAsync(cancellationToken);

            multipleLayers.AddRange(data);
        }

        var count = 0;

        foreach (var agent in agentPlayerAggregates.Where(x => x.AgentId == agentId))
        {
            var (_, mainDistributionService) = agentInstances.First(a => a.agent.Id == agent.AgentId);

            var parentAgentDividends = mainDistributionService.GetMultipleLayer(agent,
                multipleLayers,
                cancellationToken).ToArray();

            var childAgentDividends = parentAgentDividends.Where(w => w.DividendProviderAgentId != agent.AgentId);

            agent.AgentDividendAmount =
                mainDistributionService.GetAgentDividendAmount(agent, multipleLayers, cancellationToken);

            var agentModel = agents.First(x => x.Id == agentId);

            var currentNegativeAgentDividendAmount = count == 0 ? negativeAgentDividendAmount : 0M;

            var agentDividendAmount =
                agentModel.NegativeAgentDividendCarryForward == false && agent.AgentDividendAmount < 0M
                    ? 0M
                    : agent.AgentDividendAmount;

            var totalDividendAmount = currentNegativeAgentDividendAmount + agentDividendAmount;

            yield return new AgentDividendBeforehand(
                0,
                brand.CurrentSettlementFrom,
                brand.CurrentSettlementTo,
                agent.ActiveNumber,
                agent.WinLossAmount,
                agent.BonusAmount,
                agent.RebateAmount,
                agent.NetProfit() * agent.AgentDividendPercentage / 100,
                agent.AgentDividendPercentage,
                childAgentDividends.Sum(s => s.Dividend),
                totalDividendAmount,
                agent.BonusAdjustAmount,
                agent.AgentAdjustAmount,
                agent.ProductFeeAmount(),
                currentNegativeAgentDividendAmount,
                agentModel.NegativeAgentDividendCarryForward,
                null,
                null);

            count++;
        }
    }
}