using Tgs.Agent.Dividend.Client.Models;

namespace Tgs.Agent.Dividend.Client;

public interface IAgentContractClient
{
    Task CreateAgentContractAsync(CreateAgentContractRequest contract, CancellationToken cancellationToken);

    IAsyncEnumerable<AgentDividendContract?> GetAgentContractAsync(Guid agentId, CancellationToken cancellationToken);
}