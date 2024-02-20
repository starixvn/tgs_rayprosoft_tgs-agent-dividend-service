using MediatR;

namespace Tgs.Agent.Dividend.Domain.Models;

public record ClearNegativeDividend(Guid AgentId, Guid ClearByUserId) : IRequest;