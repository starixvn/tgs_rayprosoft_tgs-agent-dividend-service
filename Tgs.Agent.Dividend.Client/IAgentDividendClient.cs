using Tgs.Agent.Dividend.Client.Models;

namespace Tgs.Agent.Dividend.Client;

public interface IAgentDividendClient
{
    Task GenerateAgentDividend();
    Task GenerateAgentDividend(Guid brandId);

    Task<AgentDividendBeforehandResponse> GetAgentDividendBeforehandAsync(Guid brandId,
        Guid agentId,
        CancellationToken cancellationToken);
}