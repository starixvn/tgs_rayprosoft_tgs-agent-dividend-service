using Tgs.Agent.Dividend.Client.Models;

namespace Tgs.Agent.Dividend.Client;

public interface IBrandContractClient
{
    Task CreateBrandContractAsync(CreateBrandContract contract, CancellationToken cancellationToken);

    IAsyncEnumerable<BrandContract?> GetBrandContractAsync(Guid brandId, CancellationToken cancellationToken);
}