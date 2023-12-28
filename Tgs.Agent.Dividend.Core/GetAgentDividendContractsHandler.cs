using MediatR;
using Tgs.Agent.Dividend.Core.Workers;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;
using Tgs.Agent.Dividend.Domain.Models;
using Tgs.Agent.Dividend.Domain.Models.AgentContract;
using Tgs.Agent.Dividend.Domain.Models.BrandContract;

namespace Tgs.Agent.Dividend.Core;

internal class GetAgentDividendContractsHandler(
    IContractRepository contractRepository)
    : IStreamRequestHandler<GetAgentDividendContractsRequest, AgentDividendContractViewModel>
{
    public IAsyncEnumerable<AgentDividendContractViewModel> Handle(GetAgentDividendContractsRequest request, CancellationToken cancellationToken)
    {
        return contractRepository.GetAgentDividendContractsAsync(request.AgentId,cancellationToken);
    }
}