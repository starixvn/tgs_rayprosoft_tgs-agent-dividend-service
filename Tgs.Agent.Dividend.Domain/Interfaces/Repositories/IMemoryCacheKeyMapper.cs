using Microsoft.EntityFrameworkCore;
using Tgs.Agent.Dividend.Domain.Models;

namespace Tgs.Agent.Dividend.Domain.Interfaces.Repositories;

public interface IMemoryCacheKeyMapper
{
    public void AddCacheKey(string key);

    public void ClearAll();
}