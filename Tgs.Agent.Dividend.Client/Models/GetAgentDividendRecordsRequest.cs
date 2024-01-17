namespace Tgs.Agent.Dividend.Client.Models;

public record GetAgentDividendRecordsRequest(Guid BrandId, DateTime From, DateTime To, string? AgentUsername);