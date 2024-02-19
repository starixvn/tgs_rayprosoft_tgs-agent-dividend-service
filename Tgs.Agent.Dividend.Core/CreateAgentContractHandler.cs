//using MediatR;
//using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
//using Tgs.Agent.Dividend.Domain.Models.AgentContract;

//namespace Tgs.Agent.Dividend.Core;

//internal class CreateAgentContractHandler(
//    IContractRepository contractRepository)
//    : IRequestHandler<CreateAgentContractRequest>
//{
//    public Task Handle(CreateAgentContractRequest request, CancellationToken cancellationToken)
//    {
//        return contractRepository.CreateAgentContractAsync(request, cancellationToken);
//    }
//}