using MediatR;
using Microsoft.Extensions.Logging;
using Tgs.Agent.Dividend.Domain.Events;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.CashService.Client;

namespace Tgs.Agent.Dividend.Core.Events;

internal class SendAgentDividendEventHandler(ICashServiceClient cashServiceClient,
        IRepository repository,
        ILogger<SendAgentDividendEventHandler> logger)
    : INotificationHandler<SendAgentDividendEvent>
{
    public async Task Handle(SendAgentDividendEvent @event, CancellationToken cancellationToken)
    {
        await repository.UpdateSentOnAsync(@event.AgentDividendId, cancellationToken);

        try
        {
            _ = await cashServiceClient.AgentDividendAsync(@event.WalletId,
                @event.Amount,
                "",
                @event.AgentDividendId.ToString(),
                cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError("Request:{request} Exception:{exception}", @event, e);
        }
    }
}