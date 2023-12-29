using MediatR;

namespace Tgs.Agent.Dividend.Domain.Models.BrandContract;

public record UpdateBrandContractConditionForMultipleLayerRequest(
    Guid BrandId,
    IEnumerable<BrandContractConditionForMultipleLayerModel> Conditions) : IRequest;