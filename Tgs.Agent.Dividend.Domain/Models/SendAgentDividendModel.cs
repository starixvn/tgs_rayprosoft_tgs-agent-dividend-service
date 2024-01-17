namespace Tgs.Agent.Dividend.Domain.Models;

public record SendAgentDividendModel(int Id, Guid AgentId, decimal AgentDividendAmount, DateTime? SentOn);