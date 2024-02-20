using MediatR;
using Microsoft.Extensions.Logging;
using Tgs.Agent.Client;
using Tgs.Agent.Client.Models;
using Tgs.Agent.Dividend.Domain.Events;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.CashService.Client;

namespace Tgs.Agent.Dividend.Core.Events;

internal class SendAgentDividendByManualEventHandler(
    IAgentClient agentClient,
    ICashServiceClient cashServiceClient,
    IRepository repository,
    ILogger<SendAgentDividendByManualEventHandler> logger)
    : INotificationHandler<SendAgentDividendByManualEvent>
{
    public async Task Handle(SendAgentDividendByManualEvent @event, CancellationToken cancellationToken)
    {
        var agent = await agentClient.GetAgentInfoAsync(new GetAgentInfoRequest(@event.AgentId), cancellationToken);

        if (agent?.WalletId == null)
        {
            logger.LogError("Agent wallet not found request:{request}", @event);

            return;
        }

        try
        {
            await repository.UpdateSentOnByManualSendAsync(@event.AgentDividendId, @event.Status, @event.SendByUsername, @event.Remark, cancellationToken);

            if (@event.Amount > 0)
                _ = await cashServiceClient.AgentDividendAsync(agent.WalletId.Value,
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