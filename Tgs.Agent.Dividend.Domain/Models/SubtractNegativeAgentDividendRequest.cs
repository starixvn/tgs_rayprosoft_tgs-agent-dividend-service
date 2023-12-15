using MediatR;

namespace Tgs.Agent.Dividend.Domain.Models;

public record SubtractNegativeAgentDividendRequest(Guid AgentId, decimal AgentDividendAmount)
    : IRequest<SubtractNegativeAgentDividendModel>;