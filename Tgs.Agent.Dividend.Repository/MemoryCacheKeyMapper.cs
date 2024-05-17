using Microsoft.Extensions.Caching.Memory;
using Tgs.Agent.Dividend.Domain.Interfaces.Repositories;

namespace Tgs.Agent.Dividend.Repository;

public class MemoryCacheKeyMapper(IMemoryCache memoryCache) : IMemoryCacheKeyMapper
{
    private readonly HashSet<string> _cacheKeys = [];

    public void AddCacheKey(string key)
    {
        _cacheKeys.Add(key);
    }

    public void ClearAll()
    {
        foreach (var cacheKey in _cacheKeys) memoryCache.Remove(cacheKey);

        _cacheKeys.Clear();
    }
}