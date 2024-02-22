using MediatR;

namespace Tgs.Agent.Dividend.Domain.Models;

public record CreateAgentDividendRequest(
    Guid BrandId,
    IEnumerable<AgentDividendModel> Models,
    IDictionary<int, int> NegativeDividendsIds) : IRequest;