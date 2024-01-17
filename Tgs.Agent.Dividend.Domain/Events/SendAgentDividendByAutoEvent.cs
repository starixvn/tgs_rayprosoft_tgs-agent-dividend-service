using MediatR;

namespace Tgs.Agent.Dividend.Domain.Events;

public record SendAgentDividendByAutoEvent(
    Guid BrandId,
    Guid AgentId,
    decimal Amount,
    int AgentDividendId,
    string SendByUsername)
    : INotification;