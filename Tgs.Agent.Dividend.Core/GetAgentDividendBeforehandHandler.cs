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

        var previousAgentDividend = await repository
            .GetAgentDividendBeforehandAsync(request.AgentId, brand.PreviousSettlementFrom, brand.PreviousSettlementTo)
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
            cancellationToken).ToArray();

        var agentDividend = parentAgentDividends.FirstOrDefault(w => w.DividendProviderAgentId == agent.AgentId);

        var childAgentDividends = parentAgentDividends.Where(w => w.DividendProviderAgentId != agent.AgentId);


        agent.AgentDividendAmount =
            mainDistributionService.GetAgentDividendAmount(agent, multipleLayers, cancellationToken);

        var agentDividends = agentPlayerAggregates
            .Select(x =>
            {
                var (_, distributionService) =
                    agentInstances.First(a => a.agent.Id == x.AgentId);

                x.AgentDividendAmount =
                    distributionService.GetAgentDividendAmount(x,
                        multipleLayers,
                        cancellationToken);

                return new AgentDividendModel
                {
                    Id = x.Id,
                    BrandId = x.BrandId,
                    AgentId = x.AgentId,
                    AgentDividendAmount = x.AgentDividendAmount
                };
            });

        var sendDividend =
            await mediator.Send(new GetNoSendDividendRequest(agentDividends.ToArray()), cancellationToken);

        return new AgentDividendBeforehand(
            brand.CurrentSettlementFrom,
            brand.CurrentSettlementTo,
            agent.ActiveNumber,
            agent.WinLossAmount,
            agent.BonusAmount - agent.RebateAmount,
            agent.RebateAmount,
            agentDividend?.Dividend ?? 0M,
            agentDividend?.AgentDividendPercentage ?? 0M,
            childAgentDividends.Sum(s => s.Dividend),
            agent.AgentDividendAmount,
            agent.BonusAdjustAmount,
            parentAgentDividends.Sum(s => s.AgentCoveredBonus),
            parentAgentDividends.Sum(s => s.AgentCoveredBonusAdjust),
            agent.PlatformChargeAmount(),
            agent.ProductFeeAmount(),
            sendDividend.Model.Where(m => m.AgentId == agentId).Sum(s => s.NegativeAgentDividendAmount),
            null);
    }
}