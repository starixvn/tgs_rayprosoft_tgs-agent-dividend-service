using MediatR;

namespace Tgs.Agent.Dividend.Domain.Models;

public record GetAgentDividendBeforehandRequest(Guid BrandId, Guid AgentId) : IRequest<AgentDividendBeforehandResponse>;