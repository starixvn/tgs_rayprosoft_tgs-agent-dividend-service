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
    IRepository repository,
    IAgentClient agentClient,
    IServiceProvider serviceProvider)
    : IRequestHandler<GetAgentDividendBeforehandRequest, AgentDividendBeforehandResponse>
{
    public async Task<AgentDividendBeforehandResponse> Handle(GetAgentDividendBeforehandRequest request,
        CancellationToken cancellationToken)
    {
        var brand = await repository.GetBrandAsync(request.BrandId, cancellationToken).ConfigureAwait(false);

        var previousAgentDividend = await repository
            .GetAgentDividendBeforehandAsync(request.AgentId, brand.PreviousSettlementFrom, brand.PreviousSettlementTo)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return new AgentDividendBeforehandResponse
        {
            Previous = previousAgentDividend,
            Current = await GetCurrentAgentDividendBeforehandAsync(brand, request.AgentId, cancellationToken)
        };
    }

    private async ValueTask<AgentDividendBeforehand> GetCurrentAgentDividendBeforehandAsync(BrandModel brand,
        Guid agentId,
        CancellationToken cancellationToken)
    {
        const decimal depositHandlingFee = 0M;

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

        foreach (var instance in agentInstances)
        {
            var data = await instance.distributionService.GetAgentPlayerAggregateAsync(0,
                    brand,
                    instance.agent,
                    cancellationToken)
                .ToArrayAsync(cancellationToken);

            agentPlayerAggregates.AddRange(data);
        }

        var multipleLayers = new List<AgentDividendMultipleLayerModel>();

        foreach (var agentPlayerAggregateModel in agentPlayerAggregates)
        {
            var instance = agentInstances.First(a => a.agent.Id == agentPlayerAggregateModel.AgentId);

            var data = await instance.distributionService.GenerateMultipleLayerAsync(
                    brand,
                    instance.agent,
                    agentPlayerAggregateModel,
                    true,
                    cancellationToken)
                .ToArrayAsync(cancellationToken);

            multipleLayers.AddRange(data);
        }

        var agent = agentPlayerAggregates.First(x => x.AgentId == agentId);

        var (_, mainDistributionService) = agentInstances.First(a => a.agent.Id == agent.AgentId);

        var parentAgentDividends = mainDistributionService.GetMultipleLayer(agent,
            multipleLayers,
            cancellationToken);

        var agentDividend = parentAgentDividends.FirstOrDefault(w => w.DividendProviderAgentId == agent.AgentId);

        var childAgentDividends = parentAgentDividends.Where(w => w.DividendProviderAgentId != agent.AgentId);


        agent.AgentDividendAmount = mainDistributionService.GetAgentDividendAmount(agent, multipleLayers, cancellationToken);

        return new AgentDividendBeforehand(agent.ActiveNumber,
            agent.WinLossAmount,
            agent.BonusAmount - agent.RebateAmount,
            agent.RebateAmount,
            agent.WinLossAmount -
            agent.BonusAmount -
            agent.WinLossAmount * brand.PlatformCharge / 100,
            agent.WinLossAmount - agent.BonusAmount,
            agent.WinLossAmount * brand.PlatformCharge / 100,
            agentDividend.Dividend,
            agentDividend.AgentDividendPercentage,
            childAgentDividends.Sum(s => s.Dividend),
            agent.AgentDividendAmount,
            null);
    }
}