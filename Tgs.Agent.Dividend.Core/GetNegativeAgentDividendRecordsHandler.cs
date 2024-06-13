using MediatR;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.Core;

internal class GetNegativeAgentDividendRecordsHandler(
    IRepository repository)
    : IRequestHandler<GetNegativeAgentDividendRequest, decimal>
{
    public async Task<decimal> Handle(GetNegativeAgentDividendRequest request,
        CancellationToken cancellationToken)
    {
        return Math.Min(0M, await repository.GetNegativeAgentDividendsAsync(request.AgentId)
            .SumAsync(x => x.AgentDividendAmount, cancellationToken));
    }
}