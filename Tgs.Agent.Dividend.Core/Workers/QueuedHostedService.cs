using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Tgs.Agent.Dividend.Core.Workers;

public class QueuedHostedService(
    IBackgroundTaskQueue taskQueue,
    IServiceProvider serviceProvider,
    ILogger<QueuedHostedService> logger)
    : BackgroundService
{
    public IBackgroundTaskQueue TaskQueue = taskQueue;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Queued Hosted Service is running.{0}" +
            "{1}Tap W to add a work item to the " +
            "background queue.{2}",
            Environment.NewLine,
            Environment.NewLine,
            Environment.NewLine);

        return BackgroundProcessingAsync(stoppingToken);
    }

    private async Task BackgroundProcessingAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var workItem = await TaskQueue.DequeueAsync(stoppingToken);

            try
            {
                using var scope = serviceProvider.CreateScope();

                await workItem(scope, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred executing task work item.");
            }
        }
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Queued Hosted Service is stopping.");

        await base.StopAsync(stoppingToken);
    }
}