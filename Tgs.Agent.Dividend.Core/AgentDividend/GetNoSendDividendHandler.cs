using MediatR;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.Core.AgentDividend;

internal class GetNoSendDividendHandler(IRepository repository)
    : IRequestHandler<GetNoSendDividendRequest, NoSendDividendResponse>
{
    public async Task<NoSendDividendResponse> Handle(GetNoSendDividendRequest request, CancellationToken cancellationToken)
    {
        var deductedNegativeDividends = new Dictionary<Guid, decimal>();
        var noSendDividendIds = new Dictionary<int, int>();

        foreach (var dividend in request.AgentDividends)
        {
            var agentNoSendDividends = await repository.GetNegativeAgentDividendsAsync(dividend.AgentId)
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);

            var agentNoSendDividend = CalculateAgentNoSendDividend(dividend, deductedNegativeDividends, agentNoSendDividends);

            dividend.NegativeAgentDividendAmount = agentNoSendDividend;

            DeductNegativeDividend(dividend, agentNoSendDividend, deductedNegativeDividends, request.AgentDividends);

            if (agentNoSendDividend < 0 && dividend.AgentDividendAmount + agentNoSendDividend >= 0)
            {
                AdjustDividendAmount(dividend, agentNoSendDividend);
                UpdateNoSendDividendIds(agentNoSendDividends, noSendDividendIds, dividend);
            }

            HandleAutoSend(dividend, agentNoSendDividend);

            HandleNegativeDividendCarryForward(dividend);
        }

        return new NoSendDividendResponse { Model = request.AgentDividends, NegativeDividendsIds = noSendDividendIds };
    }

    private static decimal CalculateAgentNoSendDividend(AgentDividendModel dividend, Dictionary<Guid, decimal> deductedNegativeDividends, IEnumerable<NegativeAgentDividendModel> agentNoSendDividends)
    {
        var haveDeductedNegativeDividend = deductedNegativeDividends.TryGetValue(dividend.AgentId, out var deductedNegativeDividend);
        return haveDeductedNegativeDividend ? deductedNegativeDividend : agentNoSendDividends.Sum(x => x.AgentDividendAmount);
    }

    private static void DeductNegativeDividend(AgentDividendModel dividend, decimal agentNoSendDividend, Dictionary<Guid, decimal> deductedNegativeDividends, IEnumerable<AgentDividendModel> agentDividendModels)
    {
        var agentDividendCount = agentDividendModels.Count(x => x.AgentId == dividend.AgentId);

        if (agentDividendCount > 1)
        {
            deductedNegativeDividends[dividend.AgentId] = dividend.AgentDividendAmount + agentNoSendDividend >= 0
                ? 0
                : dividend.AgentDividendAmount + agentNoSendDividend;
        }
    }

    private static void UpdateNoSendDividendIds(IEnumerable<NegativeAgentDividendModel> agentNoSendDividends, Dictionary<int, int> noSendDividendIds, AgentDividendModel dividend)
    {
        foreach (var negativeDividend in agentNoSendDividends.Where(nd => !noSendDividendIds.ContainsKey(nd.Id)))
        {
            noSendDividendIds.Add(negativeDividend.Id, dividend.Id);
        }
    }

    private static void AdjustDividendAmount(AgentDividendModel dividend, decimal agentNoSendDividend)
    {
        dividend.BeforeDividendAmount = dividend.AgentDividendAmount;
        dividend.AgentDividendAmount = Math.Max(0, dividend.AgentDividendAmount + agentNoSendDividend);
    }

    private static void HandleAutoSend(AgentDividendModel dividend, decimal agentNoSendDividend)
    {
        if ((dividend.BeforeDividendAmount ?? dividend.AgentDividendAmount) + agentNoSendDividend < 0)
        {
            dividend.AutoSend = false;
        }
    }

    private static void HandleNegativeDividendCarryForward(AgentDividendModel dividend)
    {
        if (dividend is { NegativeAgentDividendCarryForward: false, AgentDividendAmount: < 0 })
        {
            dividend.AutoSend = true;
        }
    }
}