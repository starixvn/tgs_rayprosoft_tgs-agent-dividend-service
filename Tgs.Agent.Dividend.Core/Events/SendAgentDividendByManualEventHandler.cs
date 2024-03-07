using MediatR;
using Microsoft.Extensions.Logging;
using Tgs.Agent.Client;
using Tgs.Agent.Client.Models;
using Tgs.Agent.Dividend.Domain.Enums;
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
        var agentDividendRecord =
            await repository.GetAgentDividendRecordAsync(@event.AgentDividendRecordId, cancellationToken);

        if (agentDividendRecord == null)
        {
            logger.LogError("Can't found agentDividendRecord:{request}", @event);

            return;
        }

        var agent = await agentClient.GetAgentInfoAsync(new GetAgentInfoRequest(agentDividendRecord.AgentId),
            cancellationToken);

        if (agent?.WalletId == null)
        {
            logger.LogError("Agent wallet not found request:{request}", @event);

            return;
        }

        try
        {
            await repository.UpdateSentOnByManualSendAsync(agentDividendRecord.AgentDividendId,
                @event.Status,
                @event.SendByUsername,
                @event.Remark,
                cancellationToken);

            if (agentDividendRecord.ActualAgentDividendAmount > 0 && @event.Status == DistributeStatus.Sent)
                _ = await cashServiceClient.AgentDividendAsync(agent.WalletId.Value,
                    agentDividendRecord.ActualAgentDividendAmount,
                    "",
                    agentDividendRecord.AgentDividendId.ToString(),
                    cancellationToken);
        }
        catch (Exception e)
        {
            logger.LogError("Request:{request} Exception:{exception}", @event, e);
        }
    }
}