using System.Runtime.CompilerServices;
using MediatR;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.Core;

internal class GetAgentDividendRecordsHandler(
    IRepository repository)
    : IStreamRequestHandler<GetAgentDividendRecordsRequest, AgentDividendRecordModel>
{
    public IAsyncEnumerable<AgentDividendRecordModel> Handle(GetAgentDividendRecordsRequest request,
        CancellationToken cancellationToken)
    {
        return repository.GetAgentDividendRecordsAsync(request.BrandId,
            request.Status,
            request.From,
            request.To,
            request.ApprovedFrom,
            request.ApprovedTo,
            request.AgentUsername,
            request.NegativeAgentDividendCarryForward);
    }
}