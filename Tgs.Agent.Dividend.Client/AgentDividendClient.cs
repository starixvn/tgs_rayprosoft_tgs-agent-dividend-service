using System.Text;

namespace Tgs.Agent.Dividend.Client;

internal class AgentDividendClient(HttpClient client) : IAgentDividendClient
{
    public async Task GenerateAgentDividend()
    {
        var response = await client
            .PostAsync("/api/agentDividend", new StringContent("", Encoding.UTF8, "application/json"))
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();
    }
}