using MediatR;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.Core.AgentDividend;

internal class GetNoSendDividendHandler(IRepository repository)
    : IRequestHandler<GetNoSendDividendRequest, NoSendDividendResponse>
{
    public async Task<NoSendDividendResponse> Handle(GetNoSendDividendRequest request,
        CancellationToken cancellationToken)
    {
        var deductedNegativeDividends = new Dictionary<Guid, decimal>();

        var noSendDividendIds = new Dictionary<int, int>();

        foreach (var dividend in request.AgentDividends)
        {
            var agentNoSendDividends = await repository.GetNegativeAgentDividendsAsync(dividend.AgentId)
                .ToArrayAsync(cancellationToken)
                .ConfigureAwait(false);

            var haveDeductedNegativeDividend =
                deductedNegativeDividends.TryGetValue(dividend.AgentId, out var deductedNegativeDividend);

            var agentNoSendDividend = haveDeductedNegativeDividend
                ? deductedNegativeDividend
                : agentNoSendDividends.Sum(x => x.AgentDividendAmount);

            var agentDividendCount = request.AgentDividends
                .Count(x => x.AgentId == dividend.AgentId);

            dividend.NegativeAgentDividendAmount = agentNoSendDividend;

            if (agentDividendCount > 1)
            {
                deductedNegativeDividends[dividend.AgentId] = dividend.AgentDividendAmount + agentNoSendDividend >= 0
                    ? 0
                    : dividend.AgentDividendAmount + agentNoSendDividend;
            }

            if (agentNoSendDividend < 0 && dividend.AgentDividendAmount + agentNoSendDividend >= 0)
            {
                dividend.BeforeDividendAmount = dividend.AgentDividendAmount;

                if (dividend.AgentDividendAmount + agentNoSendDividend <= 0M)
                    dividend.AgentDividendAmount = 0M;
                else
                    dividend.AgentDividendAmount += agentNoSendDividend;

                foreach (var negativeDividend in agentNoSendDividends)
                    if (!noSendDividendIds.ContainsKey(negativeDividend.Id))
                        noSendDividendIds.Add(negativeDividend.Id, dividend.Id);
            }

            if ((dividend.BeforeDividendAmount ?? dividend.AgentDividendAmount) + agentNoSendDividend < 0)
            {
                dividend.AutoSend = false;
            }

            if (dividend is { NegativeAgentDividendCarryForward: false, AgentDividendAmount: < 0 })
            {
                dividend.AutoSend = true;
            }
        }

        return new NoSendDividendResponse { Model = request.AgentDividends, NegativeDividendsIds = noSendDividendIds };
    }
}