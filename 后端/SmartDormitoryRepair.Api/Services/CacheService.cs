using Microsoft.Extensions.Caching.Memory;

namespace SmartDormitoryRepair.Api.Services;

/// <summary>
/// 内存缓存服务
/// 用于缓存热点数据，减少数据库查询压力
/// </summary>
public class CacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<CacheService> _logger;

    public CacheService(IMemoryCache cache, ILogger<CacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// 获取缓存数据
    /// </summary>
    public T? Get<T>(string key)
    {
        if (_cache.TryGetValue(key, out T? value))
        {
            _logger.LogDebug("缓存命中: {Key}", key);
            return value;
        }

        _logger.LogDebug("缓存未命中: {Key}", key);
        return default;
    }

    /// <summary>
    /// 设置缓存数据
    /// </summary>
    public void Set<T>(string key, T value, TimeSpan? expiry = null)
    {
        var options = new MemoryCacheEntryOptions();
        
        if (expiry.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = expiry.Value;
        }
        else
        {
            options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5); // 默认5分钟
        }

        _cache.Set(key, value, options);
        _logger.LogDebug("缓存设置成功: {Key}, 过期时间: {Expiry}", key, expiry ?? TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// 删除缓存
    /// </summary>
    public void Remove(string key)
    {
        _cache.Remove(key);
        _logger.LogDebug("删除缓存: {Key}", key);
    }

    /// <summary>
    /// 获取或创建缓存
    /// </summary>
    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiry = null)
    {
        // 先尝试从缓存获取
        var cachedValue = Get<T>(key);
        if (cachedValue != null)
        {
            return cachedValue;
        }

        // 缓存未命中，执行factory获取数据
        _logger.LogDebug("缓存未命中，执行数据获取: {Key}", key);
        var value = await factory();
        
        if (value != null)
        {
            // 将数据写入缓存
            Set(key, value, expiry);
        }

        return value;
    }

    /// <summary>
    /// 清除所有匹配前缀的缓存
    /// 注意：IMemoryCache 不支持模式匹配，这里只是简单实现
    /// 生产环境建议使用 Redis
    /// </summary>
    public void RemoveByPrefix(string prefix)
    {
        _logger.LogWarning("内存缓存不支持按前缀批量删除，请手动删除相关键或使用Redis");
        // IMemoryCache doesn't support pattern matching
        // For production, consider using Redis instead
    }
}
