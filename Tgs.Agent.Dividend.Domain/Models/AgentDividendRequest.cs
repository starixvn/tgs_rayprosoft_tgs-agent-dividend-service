using MediatR;

namespace Tgs.Agent.Dividend.Domain.Models;

public record AgentDividendRequest(Guid BrandId) : IRequest;