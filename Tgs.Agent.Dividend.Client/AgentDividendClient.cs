using System.Net.Http.Json;
using System.Net.Mime;
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

    public async Task GenerateAgentDividend(CancellationToken cancellationToken)
    {
        var response = await client
            .PostAsync("/api/agentDividend",
                new StringContent("", Encoding.UTF8, "application/json"),
                cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();
    }

    public async Task GenerateAgentDividend(Guid brandId, CancellationToken cancellationToken)
    {
        var response = await client
            .PostAsync($"/api/agentDividend/{brandId}",
                new StringContent("", Encoding.UTF8, "application/json"),
                cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();
    }

    public IAsyncEnumerable<AgentDividendRecordModel?> GetAgentDividendRecordsAsync(
        GetAgentDividendRecords request,
        CancellationToken cancellationToken)
    {
        var path = ClientPath.AppendWithProperties("/api/agentDividend/records?", request);

        return client.GetFromJsonAsAsyncEnumerable<AgentDividendRecordModel>(path, cancellationToken);
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

    public async Task SendAgentDividendsAsync(Guid[] agentDividendRecordIds,
        bool approve,
        string editorUsername,
        string? remark,
        CancellationToken cancellationToken)
    {
        var data = new
        {
            AgentDividendRecordIds = agentDividendRecordIds,
            Approve = approve,
            EditorUsername = editorUsername,
            Remark = remark
        };

        var jsonContent = new StringContent(
            JsonSerializer.Serialize(data),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        var response = await client
            .PostAsync("/api/agentDividend/send-agent-dividend",
                jsonContent,
                cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();
    }

    public async Task ClearAgentNegativeDividendAsync(Guid agentId, Guid clearByUserId, CancellationToken cancellationToken = default)
    {
        var response = await client
            .PutAsync($"/api/agentDividend/{agentId}/clear-negative-dividend/{clearByUserId}", null, cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();
    }

    public async Task<decimal> GetNegativeAgentDividendAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        var response = await client
            .GetAsync($"/api/agentDividend/{agentId}/negative-dividend", cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();

        return Convert.ToDecimal(await response.Content.ReadAsStringAsync(cancellationToken));
    }

    public IAsyncEnumerable<AgentDividendModel?> GetPreCalculateAgentDividend(Guid brandId,
        CancellationToken cancellationToken)
    {
        return client.GetFromJsonAsAsyncEnumerable<AgentDividendModel>($"/api/agentDividend/{brandId}/pre-calculate",
            cancellationToken);
    }
}