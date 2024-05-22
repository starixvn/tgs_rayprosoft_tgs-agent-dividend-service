using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
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

                if (!await repository.GetNonArchivedBrandIdsAsyncEnumerable(model.BrandId)
                        .AnyAsync(stoppingToken)) continue;

                var brand = await repository.GetBrandAsync(model.BrandId, stoppingToken).ConfigureAwait(false);

                var days = brand.CurrentSettlementTo.AddDays(1).Subtract(brand.CurrentSettlementFrom).Days;

                var count = await repository.GetCompletePlayerAggregateCountAsync(brand.CurrentSettlementFrom,
                    brand.CurrentSettlementTo.AddDays(1),
                    stoppingToken);

                if (days * 24 != count)
                {
                    continue;
                }

                var response = await mediator.Send(new AgentDividendRequest(model.BrandId, false), stoppingToken);

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