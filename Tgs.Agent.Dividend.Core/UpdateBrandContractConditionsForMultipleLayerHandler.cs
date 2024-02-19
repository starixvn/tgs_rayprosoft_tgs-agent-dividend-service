//using MediatR;
//using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
//using Tgs.Agent.Dividend.Domain.Models.BrandContract;

//namespace Tgs.Agent.Dividend.Core;

//internal class UpdateBrandContractConditionsForMultipleLayerHandler(
//    IContractRepository contractRepository)
//    : IRequestHandler<UpdateBrandContractConditionForMultipleLayerRequest>
//{
//    public Task Handle(UpdateBrandContractConditionForMultipleLayerRequest request, CancellationToken cancellationToken)
//    {
//        return contractRepository.UpdateBrandContractConditionsForMultipleLayerAsync(request, cancellationToken);
//    }
//}