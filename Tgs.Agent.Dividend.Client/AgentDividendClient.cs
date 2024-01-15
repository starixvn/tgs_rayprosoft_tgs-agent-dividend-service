using System.Text;
using System.Text.Json;
using Tgs.Agent.Dividend.Client.Models;

namespace Tgs.Agent.Dividend.Client;

internal class AgentDividendClient(HttpClient client) : IAgentDividendClient
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task GenerateAgentDividend()
    {
        var response = await client
            .PostAsync("/api/agentDividend", new StringContent("", Encoding.UTF8, "application/json"))
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();
    }

    public async Task GenerateAgentDividend(Guid brandId)
    {
        var response = await client
            .PostAsync($"/api/agentDividend/{brandId}", new StringContent("", Encoding.UTF8, "application/json"))
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();
    }

    public async Task<AgentDividendBeforehandResponse> GetAgentDividendBeforehandAsync(Guid brandId,
        Guid agentId,
        CancellationToken cancellationToken)
    {
        var response = await client
            .GetAsync($"/api/agentDividend/{brandId}/{agentId}/beforehand", cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<AgentDividendBeforehandResponse>(
            await response.Content.ReadAsStringAsync(cancellationToken),
            JsonSerializerOptions)!;
    }
}