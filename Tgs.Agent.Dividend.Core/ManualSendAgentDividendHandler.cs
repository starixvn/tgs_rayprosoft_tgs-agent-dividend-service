using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Tgs.Agent.Dividend.Core.Workers;
using Tgs.Agent.Dividend.Domain.Enums;
using Tgs.Agent.Dividend.Domain.Events;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.Core;

internal class ManualSendAgentDividendHandler(
    IBackgroundTaskQueue backgroundTaskQueue)
    : IRequestHandler<ManualSendAgentDividend>
{
    public async Task Handle(ManualSendAgentDividend request, CancellationToken cancellationToken)
    {
        await backgroundTaskQueue.QueueBackgroundWorkItemAsync(async (scope, token) =>
        {
            var innerMediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            await innerMediator.Publish(new SendAgentDividendByManualEvent(
                    request.AgentDividendRecordId,
                    request.EditorUsername,
                    request.Approve ? DistributeStatus.Sent : DistributeStatus.Rejected,
                    request.Remark),
                token);
        });
    }
}