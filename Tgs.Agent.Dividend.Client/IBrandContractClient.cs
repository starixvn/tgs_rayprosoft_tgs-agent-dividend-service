using Tgs.Agent.Dividend.Client.Models;

namespace Tgs.Agent.Dividend.Client;

public interface IBrandContractClient
{
    Task CreateBrandContractAsync(CreateBrandContract contract, CancellationToken cancellationToken);

    Task UpdateBrandContractConditionsForMultipleLayerAsync(Guid brandId,
        IEnumerable<BrandContractConditionForMultipleLayerModel> conditions,
        CancellationToken cancellationToken);

    IAsyncEnumerable<BrandContract?> GetBrandContractAsync(Guid brandId, CancellationToken cancellationToken);
}