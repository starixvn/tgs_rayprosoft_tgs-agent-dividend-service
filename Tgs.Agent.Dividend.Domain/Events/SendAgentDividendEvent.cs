using MediatR;

namespace Tgs.Agent.Dividend.Domain.Events;

public record SendAgentDividendEvent
    (Guid BrandId, Guid AgentId, Guid WalletId, decimal Amount, int AgentDividendId) : INotification;