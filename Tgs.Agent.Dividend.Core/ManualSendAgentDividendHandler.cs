using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Tgs.Agent.Dividend.Core.Workers;
using Tgs.Agent.Dividend.Domain.Events;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.Core;

internal class ManualSendAgentDividendHandler(
    IMediator mediator,
    IBackgroundTaskQueue backgroundTaskQueue,
    IRepository repository)
    : IRequestHandler<ManualSendAgentDividendRequest>
{
    public async Task Handle(ManualSendAgentDividendRequest request, CancellationToken cancellationToken)
    {
        var agentDividendRecord =
            await repository.GetAgentDividendRecordAsync(request.AgentDividendRecordId, cancellationToken);

        if (agentDividendRecord is { SentOn: null })
        {
            await backgroundTaskQueue.QueueBackgroundWorkItemAsync(async (scope, token) =>
            {
                var innerMediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                await innerMediator.Publish(new SendAgentDividendByManualEvent(agentDividendRecord.BrandId,
                        agentDividendRecord.AgentId,
                        agentDividendRecord.AgentDividendAmount,
                        agentDividendRecord.AgentDividendId,
                        "system"),
                    token);
            });
        }
    }
}