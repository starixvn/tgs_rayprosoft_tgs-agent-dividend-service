using MediatR;
using Tgs.Agent.Dividend.Domain.Enums;

namespace Tgs.Agent.Dividend.Domain.Events;

public record SendAgentDividendByManualEvent(
    Guid BrandId,
    Guid AgentId,
    decimal Amount,
    int AgentDividendId,
    string SendByUsername,
    DistributeStatus Status,
    string? Remark)
    : INotification;