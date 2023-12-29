namespace Tgs.Agent.Dividend.Client.Models;

public record CreateAgentContractRequest(
    Guid BrandId,
    Guid AgentId,
    Guid BrandContractId,
    string Name);