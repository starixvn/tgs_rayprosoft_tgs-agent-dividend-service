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
        var brand = await repository.GetBrandAsync(request.BrandId, cancellationToken).ConfigureAwait(false);

        var (nextFrom, nextTo) = DateTimeOffsetExtensions.GetInterval(
            (SettlementIntervalType)brand.SettlementIntervalType,
            new DateTimeOffset(brand.NextSettlementTo).AddDays(1).ToOffset(new TimeSpan(8, 0, 0)),
            brand.SettlementDay);

        brand.PreviousSettlementFrom = brand.CurrentSettlementFrom;
        brand.PreviousSettlementTo = brand.CurrentSettlementTo;
        brand.CurrentSettlementFrom = brand.NextSettlementFrom;
        brand.CurrentSettlementTo = brand.NextSettlementTo;
        brand.NextSettlementFrom = nextFrom.UtcDateTime;
        brand.NextSettlementTo = nextTo.UtcDateTime;

        var records = request.Models.Select(x =>
        {
            var status = x is { AgentDividendAmount: <= 0, NegativeAgentDividendCarryForward: false }
                ? DistributeStatus.Sent
                : DistributeStatus.ReadyToApprove;

            var approvedOn = x is { AgentDividendAmount: <= 0, NegativeAgentDividendCarryForward: false }
                ? DateTime.UtcNow
                : (DateTime?)null;

            return new CreateAgentDividendRecordsModel
            {
                BrandId = brand.Id,
                AgentId = x.AgentId,
                Status = status,
                AgentDividendId = x.Id,
                AgentDividendAmount = x.AgentDividendAmount,
                ActualAgentDividendAmount = x.AgentDividendAmount + x.NegativeAgentDividendAmount,
                NegativeAgentDividendAmount = x.NegativeAgentDividendAmount,
                NegativeAgentDividendCarryForward = x.NegativeAgentDividendCarryForward,
                CreatedOn = DateTime.UtcNow,
                UpdatedOn = DateTime.UtcNow,
                ApprovedOn = approvedOn,
                SentByUsername = approvedOn is not null ? "system" : null
            };
        });

        await repository.CreateAgentDividendsAsync(brand,
            request.Models,
            records,
            request.NegativeDividendsIds,
            cancellationToken);

        foreach (var createAgentDividendModel in request.Models.Where(x => x.AutoSend))
        {
            await backgroundTaskQueue.QueueBackgroundWorkItemAsync(async (scope, token) =>
            {
                var innerMediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                await innerMediator.Publish(new SendAgentDividendByAutoEvent(brand.Id,
                        createAgentDividendModel.AgentId,
                        createAgentDividendModel.AgentDividendAmount,
                        createAgentDividendModel.Id,
                        "system"),
                    token);
            });
        }
    }
}