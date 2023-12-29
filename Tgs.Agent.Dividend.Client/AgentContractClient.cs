using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Tgs.Agent.Dividend.Client.Models;

namespace Tgs.Agent.Dividend.Client;

public class AgentContractClient(HttpClient client) : IAgentContractClient
{
    public async Task CreateAgentContractAsync(CreateAgentContractRequest contract, CancellationToken cancellationToken)
    {
        using StringContent jsonContent = new(
            JsonSerializer.Serialize(contract),
            Encoding.UTF8,
            "application/json");

        var response = await client
            .PostAsync("/api/agentContract", jsonContent, cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();
    }

    public IAsyncEnumerable<AgentDividendContract?> GetAgentContractAsync(Guid agentId,
        CancellationToken cancellationToken)
    {
        return client.GetFromJsonAsAsyncEnumerable<AgentDividendContract>($"/api/agentContract/{agentId}",
            cancellationToken);
    }
}