using MediatR;
using Tgs.Agent.Dividend.Domain.Enums;

namespace Tgs.Agent.Dividend.Domain.Events;

public record SendAgentDividendByManualEvent(
    Guid AgentDividendRecordId,
    string SendByUsername,
    DistributeStatus Status,
    string? Remark)
    : INotification;