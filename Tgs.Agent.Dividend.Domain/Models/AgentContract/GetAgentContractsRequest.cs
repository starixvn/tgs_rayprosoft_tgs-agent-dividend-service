using MediatR;

namespace Tgs.Agent.Dividend.Domain.Models.AgentContract;

public record GetAgentDividendContractsRequest(Guid AgentId) : IStreamRequest<AgentDividendContractViewModel>;