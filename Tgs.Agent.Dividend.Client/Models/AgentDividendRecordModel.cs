namespace Tgs.Agent.Dividend.Client.Models;

public record AgentDividendRecordModel(
    Guid Id,
    Guid BrandId,
    Guid AgentId,
    int AgentDividendId,
    string AgentUsername,
    decimal AgentDividendAmount,
    DateTime CreatedOn,
    DateTime? SentOn);