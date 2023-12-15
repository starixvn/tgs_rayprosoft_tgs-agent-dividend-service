namespace Tgs.Agent.Dividend.Domain.Models;

public record SubtractNegativeAgentDividendModel(Guid AgentId,
    decimal AgentDividendAmount,
    decimal SubtractNegativeAgentDividendAmount,
    int[] Ids);