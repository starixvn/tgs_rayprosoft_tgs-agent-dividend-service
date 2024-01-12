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

        var previousAgentDividend = await repository
            .GetAgentDividendsAsync(request.AgentId, brand.PreviousSettlementFrom, brand.PreviousSettlementTo)
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

        var agents = await agentClient
            .GetChildAgentInfosAsync(new GetChildAgentInfosRequest(agentId), cancellationToken)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var agentIds = agents.Select(x => x.AgentId).Append(agentId).ToArray();

        var contracts = await contractRepository.GetBrandAgentDividendContractsAsync(brand.Id)
            .Where(x => x.AgentId == agentId || agents.Select(s => s.AgentId).Contains(x.AgentId))
            .ToArrayAsync(cancellationToken);

        var agentContracts = agentIds
            .Select(id =>
            {
                var agentContract = contracts.FirstOrDefault(a =>
                                        a.AgentId == id &&
                                        a.ContractStatus == (short)ContractStatus.Enabled) ??
                                    throw new Exception(
                                        $"Can't find contract agentId: {id}");

                return agentContract;
            })
            .ToArray();

        var agentPlayerAggregates = new List<AgentPlayerAggregateModel>();

        foreach (var agentContract in agentContracts)
        {
            var data = await dividendCalculator.GetAgentPlayerAggregateAsync(0,
                    brand,
                    agentContract,
                    cancellationToken)
                .ToArrayAsync(cancellationToken);

            agentPlayerAggregates.AddRange(data);
        }

        var multipleLayers = new List<AgentDividendMultipleLayerModel>();

        foreach (var agentPlayerAggregateModel in agentPlayerAggregates)
        {
            var agentContract = agentContracts.First(a => a.AgentId == agentPlayerAggregateModel.AgentId);

            var data = await dividendCalculator.GenerateMultipleLayerAsync(
                    brand,
                    agentContract,
                    agentPlayerAggregateModel,
                    cancellationToken)
                .ToArrayAsync(cancellationToken);

            multipleLayers.AddRange(data);
        }

        var agent = agentPlayerAggregates.First(x => x.AgentId == agentId);

        var parentAgentDividends = dividendCalculator.GetMultipleLayer(agent,
            multipleLayers,
            cancellationToken);

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
            agentDividend.Dividend,
            agentDividend.AgentDividendPercentage,
            childAgentDividends.Sum(s => s.Dividend),
            agent.AgentDividendAmount,
            null);
    }
}