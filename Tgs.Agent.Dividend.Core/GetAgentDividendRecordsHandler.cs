using MediatR;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;
using Tgs.Agent.Dividend.Domain.Models.BrandContract;

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