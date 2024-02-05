using Tgs.Agent.Dividend.Domain.Models;
using Tgs.Agent.Dividend.Domain.Models.AgentContract;
using Tgs.Agent.Dividend.Domain.Models.BrandContract;

namespace Tgs.Agent.Dividend.Domain.Interfaces.Repositories;

public interface IContractRepository
{
    IAsyncEnumerable<AgentDividendContractModel> GetAgentDividendContractsByBrandIdAsync(Guid brandId);

    IAsyncEnumerable<AgentDividendContractConditionModel> GetAgentDividendContractConditionsAsync(
        Guid agentDividendContractId,
        CancellationToken cancellationToken);

    IAsyncEnumerable<BrandContractModel> GetBrandContractsAsync(Guid brandId);

    IAsyncEnumerable<AgentDividendContractViewModel> GetAgentDividendContractsAsync(Guid agentId,
        CancellationToken cancellationToken);

    Task CreateAgentContractAsync(CreateAgentContractRequest request,
        CancellationToken cancellationToken);

    Task CreateBrandContractAsync(CreateBrandContractRequest request,
        CancellationToken cancellationToken);

    Task UpdateBrandContractConditionsForMultipleLayerAsync(UpdateBrandContractConditionForMultipleLayerRequest request,
        CancellationToken cancellationToken);

    Task CreateAgentContractSnapshotAsync(Guid id, string data, CancellationToken cancellationToken);
}