namespace Tgs.Agent.Dividend.Domain.Models;

public record NegativeAgentDividendModel(
    int Id,
    Guid AgentId,
    decimal AgentDividendAmount);