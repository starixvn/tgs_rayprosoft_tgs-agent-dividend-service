using MediatR;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models.BrandContract;

namespace Tgs.Agent.Dividend.Core;

internal class GetBrandContractsHandler(
    IContractRepository contractRepository)
    : IStreamRequestHandler<GetBrandContractsRequest, BrandContractModel>
{
    public IAsyncEnumerable<BrandContractModel> Handle(GetBrandContractsRequest request,
        CancellationToken cancellationToken)
    {
        return contractRepository.GetBrandContractsAsync(request.BrandId);
    }
}