using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Tgs.Agent.Dividend.Core.Workers;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.Core;

internal class ClearNegativeCommissionHandler(
    IBackgroundTaskQueue backgroundTaskQueue)
    : IRequestHandler<ClearNegativeDividend>
{
    public async Task Handle(ClearNegativeDividend request, CancellationToken cancellationToken)
    {
        await backgroundTaskQueue.QueueBackgroundWorkItemAsync(async (scope, token) =>
        {
            var innerRepository = scope.ServiceProvider.GetRequiredService<IRepository>();

            await innerRepository.ClearNegativeCommissionAsync(request.AgentId, request.ClearByUserId, token);
        });
    }
}