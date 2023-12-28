using MediatR;

namespace Tgs.Agent.Dividend.Domain.Models.BrandContract;

public record GetBrandContractsRequest(Guid BrandId) : IStreamRequest<BrandContractModel>;