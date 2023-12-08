using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.Domain.Interfaces.Repositories;

public interface IContractRepository
{
    IAsyncEnumerable<AgentDividendContractModel> GetAgentDividendContractsAsync(Guid brandId);

    IAsyncEnumerable<AgentDividendContractPercentageModel> GetAgentDividendContractConditionsAsync(
        Guid agentDividendContractId,
        CancellationToken cancellationToken);
}