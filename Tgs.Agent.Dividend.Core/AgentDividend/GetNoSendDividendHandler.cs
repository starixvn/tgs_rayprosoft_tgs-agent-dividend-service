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

            var agentDividendAmount = request.AgentDividends.Where(x => x.AgentId == dividend.AgentId)
                .Sum(x => x.AgentDividendAmount);

            switch (agentDividendAmount + agentNoSendDividend)
            {
                case > 0M:
                {
                    dividend.BeforeDividendAmount = dividend.AgentDividendAmount;

                    deductedNegativeDividends[dividend.AgentId] = dividend.AgentDividendAmount + agentNoSendDividend;

                    if (dividend.AgentDividendAmount + agentNoSendDividend <= 0M)
                        dividend.AgentDividendAmount = 0M;
                    else
                        dividend.AgentDividendAmount += agentNoSendDividend;

                    foreach (var negativeDividend in agentNoSendDividends)
                        if (!noSendDividendIds.ContainsKey(negativeDividend.Id))
                            noSendDividendIds.Add(negativeDividend.Id, dividend.Id);
                    break;
                }
                case < 0M:
                    dividend.AutoSend = false;
                    break;
            }
        }

        return new NoSendDividendResponse { Model = request.AgentDividends, NegativeDividendsIds = noSendDividendIds };
    }
}