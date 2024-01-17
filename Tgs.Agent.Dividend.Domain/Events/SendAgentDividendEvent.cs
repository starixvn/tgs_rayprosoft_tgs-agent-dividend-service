using MediatR;

namespace Tgs.Agent.Dividend.Domain.Events;

public record SendAgentDividendEvent(
    Guid BrandId,
    Guid AgentId,
    decimal Amount,
    int AgentDividendId,
    string SendByUsername)
    : INotification;