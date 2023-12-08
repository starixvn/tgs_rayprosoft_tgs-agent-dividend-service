using MediatR;

namespace Tgs.Agent.Dividend.Domain.Models;

public record GenerateAgentDividendRequest(Guid? BrandId = null) : IRequest;