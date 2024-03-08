using System.Runtime.CompilerServices;
using MediatR;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;
using Tgs.Agent.Dividend.Domain.Models.BrandContract;

namespace Tgs.Agent.Dividend.Core;

internal class GetAgentDividendRecordsHandler(
    IRepository repository)
    : IStreamRequestHandler<GetAgentDividendRecordsRequest, AgentDividendRecordModel>
{
    public async IAsyncEnumerable<AgentDividendRecordModel> Handle(GetAgentDividendRecordsRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var records = await repository.GetAgentDividendRecordsAsync(request.BrandId,
            request.Status,
            request.From,
            request.To,
            request.ApprovedFrom,
            request.ApprovedTo,
            request.AgentUsername,
            request.NegativeAgentDividendCarryForward);

        foreach (var agentDividendRecordModel in records)
        {
            yield return agentDividendRecordModel;
        }
    }
}