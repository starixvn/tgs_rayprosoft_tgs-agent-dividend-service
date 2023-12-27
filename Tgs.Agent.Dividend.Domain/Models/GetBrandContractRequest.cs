using MediatR;

namespace Tgs.Agent.Dividend.Domain.Models;

public record GetBrandContractRequest(Guid? BrandId) : IStreamRequest<BrandContract>;