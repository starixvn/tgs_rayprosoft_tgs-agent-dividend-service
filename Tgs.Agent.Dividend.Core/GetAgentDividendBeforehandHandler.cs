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
    IContractRepository contractRepository,
    IRepository repository,
    IAgentClient agentClient,
    [FromKeyedServices("OnePercentage")] IDividendCalculator dividendCalculator)
    : IRequestHandler<GetAgentDividendBeforehandRequest, AgentDividendBeforehandResponse>
{
    public async Task<AgentDividendBeforehandResponse> Handle(GetAgentDividendBeforehandRequest request,
        CancellationToken cancellationToken)
    {
        var brand = await repository.GetBrandAsync(request.BrandId, cancellationToken).ConfigureAwait(false);

        var agents = await repository.GetAgentsCacheAsyncEnumerable(request.BrandId, cancellationToken)
            .FirstAsync(x => x.Id == request.AgentId, cancellationToken)
            .ConfigureAwait(false);

        var previousAgentDividend = await repository
            .GetAgentDividendBeforehandAsync(request.AgentId, agents.PreviousSettlementFrom, agents.PreviousSettlementTo)
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

        var contracts = await contractRepository.GetBrandAgentDividendContractsAsync(brand.Id)
            .Where(x => x.AgentId == agentId || childAgents.Select(s => s.AgentId).Contains(x.AgentId))
            .ToArrayAsync(cancellationToken);

        var agentInstances = await repository.GetAgentsCacheAsyncEnumerable(brand.Id, cancellationToken).Where(x => agentIds.Contains(x.Id))
            .Select(x =>
            {
                var agentContract = contracts.FirstOrDefault(a =>
                                        a.AgentId == x.Id &&
                                        a.ContractStatus == (short)ContractStatus.Enabled) ??
                                    throw new Exception(
                                        $"Can't find contract agentId: {x.Id}");

                return (agentContract, agent: x);
            })
            .ToArrayAsync(cancellationToken);

        var agentPlayerAggregates = new List<AgentPlayerAggregateModel>();

        foreach (var instance in agentInstances)
        {
            var data = await dividendCalculator.GetAgentPlayerAggregateAsync(0,
                    instance.agent,
                    instance.agentContract,
                    cancellationToken)
                .ToArrayAsync(cancellationToken);

            agentPlayerAggregates.AddRange(data);
        }

        var multipleLayers = new List<AgentDividendMultipleLayerModel>();

        foreach (var agentPlayerAggregateModel in agentPlayerAggregates)
        {
            var instance = agentInstances.First(a => a.agent.Id == agentPlayerAggregateModel.AgentId);

            var data = await dividendCalculator.GenerateMultipleLayerAsync(
                    brand,
                    instance.agentContract,
                    agentPlayerAggregateModel,
                    cancellationToken)
                .ToArrayAsync(cancellationToken);

            multipleLayers.AddRange(data);
        }

        var agent = agentPlayerAggregates.First(x => x.AgentId == agentId);

        var parentAgentDividends = dividendCalculator.GetMultipleLayer(agent,
            multipleLayers,
            cancellationToken)
            .ToArray();

        var agentDividend = parentAgentDividends.FirstOrDefault(w => w.DividendProviderAgentId == agent.AgentId);

        var childAgentDividends = parentAgentDividends.Where(w => w.DividendProviderAgentId != agent.AgentId);


        agent.AgentDividendAmount = dividendCalculator.GetAgentDividendAmount(agent, multipleLayers, cancellationToken);

        return new AgentDividendBeforehand(agent.ActiveNumber,
            agent.WinLossAmount,
            agent.BonusAmount - agent.RebateAmount,
            agent.RebateAmount,
            agent.WinLossAmount -
            agent.BonusAmount -
            depositHandlingFee -
            agent.WinLossAmount * brand.PlatformCharge / 100,
            agent.WinLossAmount - agent.BonusAmount,
            depositHandlingFee,
            agent.WinLossAmount * brand.PlatformCharge / 100,
            agentDividend?.Dividend ?? 0M,
            agentDividend?.AgentDividendPercentage ?? 0M,
            childAgentDividends.Sum(s => s.Dividend),
            agent.AgentDividendAmount,
            null);
    }
}