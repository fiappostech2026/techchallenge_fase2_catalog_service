using System.Text.Json;
using FCG.Catalog.Domain.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace FCG.Catalog.Infra.Cache;

public sealed class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;

    public RedisCacheService(IDistributedCache cache)
    {
        ArgumentNullException.ThrowIfNull(cache);

        _cache = cache;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var json = await _cache.GetStringAsync(key);
        return json is null ? default : JsonSerializer.Deserialize<T>(json);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan expiration)
    {
        var json = JsonSerializer.Serialize(value);
        return _cache.SetStringAsync(key, json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration
        });
    }

    public Task RemoveAsync(string key) => _cache.RemoveAsync(key);
}
