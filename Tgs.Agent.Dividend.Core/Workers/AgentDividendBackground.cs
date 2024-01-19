using System.Threading;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.Core.Workers;

public class AgentDividendBackground(
    IAgentDividendTaskQueue taskQueue,
    ILogger<AgentDividendBackground> logger,
    IServiceProvider serviceProvider)
    : BackgroundService
{
    public IAgentDividendTaskQueue TaskQueue = taskQueue;

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Queued Hosted Service is stopping.");

        await base.StopAsync(stoppingToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Queued Hosted Service is running.{0}" +
            "{1}Tap W to add a work item to the " +
            "background queue.{2}",
            Environment.NewLine,
            Environment.NewLine,
            Environment.NewLine);

        await BackgroundProcessingAsync(stoppingToken);
    }

    private async Task BackgroundProcessingAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var model = await TaskQueue.DequeueAsync(stoppingToken);

            try
            {
                using var scope = serviceProvider.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                var repository = scope.ServiceProvider.GetRequiredService<IRepository>();

                if (!await repository.GetNonArchivedBrandIdsAsyncEnumerable(model.BrandId).AnyAsync(stoppingToken))
                {
                    continue;
                }

                var response = await mediator.Send(new AgentDividendRequest(model.BrandId), stoppingToken);

                await mediator.Send(
                    new CreateAgentDividendRequest(model.BrandId, response.Model, response.NegativeDividendsIds),
                    stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred executing task work item.");
            }
        }
    }
}