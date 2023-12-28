using MediatR;
using Tgs.Agent.Dividend.Core.Workers;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models.BrandContract;

namespace Tgs.Agent.Dividend.Core;

internal class CreateBrandContractHandler(
    IContractRepository contractRepository)
    : IRequestHandler<CreateBrandContractRequest>
{
    public Task Handle(CreateBrandContractRequest request, CancellationToken cancellationToken)
    {
        return contractRepository.CreateBrandContractAsync(request, cancellationToken);
    }
}