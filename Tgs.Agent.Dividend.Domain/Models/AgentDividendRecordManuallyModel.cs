namespace Tgs.Agent.Dividend.Domain.Models;

public record AgentDividendRecordManuallyModel(
    Guid Id,
    Guid BrandId,
    Guid AgentId,
    int AgentDividendId,
    decimal AgentDividendAmount,
    decimal ActualAgentDividendAmount,
    decimal NegativeAgentDividendAmount,
    bool NegativeAgentDividendCarryForward,
    DateTime CreatedOn,
    DateTime UpdatedOn,
    DateTime? ApprovedOn);