using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Tgs.Agent.Dividend.Core.Workers;
using Tgs.Agent.Dividend.Domain;
using Tgs.Agent.Dividend.Domain.Enums;
using Tgs.Agent.Dividend.Domain.Events;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.Core.AgentDividend;

internal class CreateAgentDividendHandler(
    IRepository repository,
    IBackgroundTaskQueue backgroundTaskQueue)
    : IRequestHandler<CreateAgentDividendRequest>
{
    public async Task Handle(CreateAgentDividendRequest request, CancellationToken cancellationToken)
    {
        await repository.CreateAgentDividendsAsync(request.BrandId,
            request.Model,
            request.NegativeDividendsIds,
            cancellationToken);

        foreach (var createAgentDividendModel in request.Model.Where(x => x.AutoSend))
        {
            await backgroundTaskQueue.QueueBackgroundWorkItemAsync(async (scope, token) =>
            {
                var innerMediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                await innerMediator.Publish(new SendAgentDividendByAutoEvent(request.BrandId,
                        createAgentDividendModel.AgentId,
                        createAgentDividendModel.AgentDividendAmount,
                        createAgentDividendModel.Id,
                        "system"),
                    token);
            });
        }
    }
}