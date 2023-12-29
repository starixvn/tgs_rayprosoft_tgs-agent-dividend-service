using MediatR;

namespace Tgs.Agent.Dividend.Domain.Models.AgentContract;

public record CreateAgentContractRequest(
    Guid BrandId,
    Guid AgentId,
    Guid BrandContractId,
    string Name) : IRequest;