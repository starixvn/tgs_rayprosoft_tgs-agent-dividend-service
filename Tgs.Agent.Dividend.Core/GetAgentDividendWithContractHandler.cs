//using System.Diagnostics.Contracts;
//using MediatR;
//using Microsoft.Extensions.DependencyInjection;
//using Tgs.Agent.Dividend.Domain.Enums;
//using Tgs.Agent.Dividend.Domain.Events;
//using Tgs.Agent.Dividend.Domain.Interfaces;
//using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
//using Tgs.Agent.Dividend.Domain.Models;

//namespace Tgs.Agent.Dividend.Core
//{
//    internal class GetAgentDividendWithContractHandler(IKeyedServiceProvider serviceProvider)
//        : IStreamRequestHandler<GetAgentDividendWithContractRequest, CreateAgentDividendModel>
//    {
//        public IAsyncEnumerable<CreateAgentDividendModel> Handle(GetAgentDividendWithContractRequest request, CancellationToken cancellationToken)
//        {
//            var distributionService = serviceProvider.GetRequiredKeyedService<IDistribution>((ContractDividendType)request.Contract.ContractDividendType);

//            return request.Aggregate.SelectAwait(async agentPlayerData =>
//            {
//                var agentDividendMultipleLayer = distributionService.AgentDividendGenerationAsync(request.Id,
//                    request.Brand,
//                    request.Contract,
//                    agentPlayerData,
//                    cancellationToken);

//                return await distributionService.GetDividendAsync(request.Brand, agentPlayerData, agentDividendMultipleLayer, cancellationToken);
//            }).AsAsyncEnumerable();
//        }
//    }
//}

