using MediatR;

namespace Tgs.Agent.Dividend.Domain.Models;

public record GetNegativeAgentDividendRequest(Guid AgentId) : IRequest<decimal>;