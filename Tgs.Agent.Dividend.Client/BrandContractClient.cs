using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Tgs.Agent.Dividend.Client.Models;

namespace Tgs.Agent.Dividend.Client;

public class BrandContractClient(HttpClient client) : IBrandContractClient
{
    public async Task CreateBrandContractAsync(CreateBrandContract contract, CancellationToken cancellationToken)
    {
        using StringContent jsonContent = new(
            JsonSerializer.Serialize(contract),
            Encoding.UTF8,
            "application/json");

        var response = await client
            .PostAsync("/api/brandContract", jsonContent, cancellationToken)
            .ConfigureAwait(false);

        _ = response.EnsureSuccessStatusCode();
    }

    public IAsyncEnumerable<BrandContract?> GetBrandContractAsync(Guid brandId, CancellationToken cancellationToken)
    {
        return client.GetFromJsonAsAsyncEnumerable<BrandContract>("/api/brandContract", cancellationToken);
    }
}