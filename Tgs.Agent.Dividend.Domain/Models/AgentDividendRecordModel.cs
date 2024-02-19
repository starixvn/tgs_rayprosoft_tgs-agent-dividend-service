using Tgs.Agent.Dividend.Domain.Enums;

namespace Tgs.Agent.Dividend.Domain.Models;

public record AgentDividendRecordModel(
    Guid Id,
    Guid BrandId,
    Guid AgentId,
    DistributeStatus Status,
    DateTime From,
    DateTime To,
    int AgentDividendId,
    string AgentUsername,
    decimal AgentDividendAmount,
    decimal ActualAgentDividendAmount,
    decimal NegativeAgentDividendAmount,
    bool NegativeAgentDividendCarryForward,
    DateTime CreatedOn,
    DateTime UpdatedOn,
    DateTime? ApprovedOn,
    string? Remark);