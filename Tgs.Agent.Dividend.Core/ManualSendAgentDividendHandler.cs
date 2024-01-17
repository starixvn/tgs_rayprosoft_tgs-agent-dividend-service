using MediatR;
using Tgs.Agent.Dividend.Domain.Events;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.Core;

internal class ManualSendAgentDividendHandler(
    IMediator mediator,
    IRepository repository)
    : IRequestHandler<ManualSendAgentDividendRequest>
{
    public async Task Handle(ManualSendAgentDividendRequest request, CancellationToken cancellationToken)
    {
        var agentDividendRecord =
            await repository.GetAgentDividendRecordAsync(request.AgentDividendRecordId, cancellationToken);

        if (agentDividendRecord is { SentOn: null })
            await mediator.Publish(new SendAgentDividendEvent(agentDividendRecord.BrandId,
                    agentDividendRecord.AgentId,
                    agentDividendRecord.AgentDividendAmount,
                    agentDividendRecord.AgentDividendId,
                    "system"),
                cancellationToken);
    }
}