namespace HRM.BuildingBlocks.Application.Abstractions.Caching;

/// <summary>
/// Abstraction over caching backends (IMemoryCache or IDistributedCache/Redis).
/// Configured in appsettings.json via CacheSettings.Provider = "Memory" | "Redis".
///
/// Cache Key Convention (Multi-Tenant Safe):
///   {module}:{tenantId}:{resource}:{id}
///   Example: identity:{tenantId}:permissions:{accountId}
///
/// Tenant-aware Invalidation:
///   await cache.RemoveByPrefixAsync($"identity:{tenantId}:permissions:");
///
/// Note on RemoveByPrefixAsync with Redis:
///   Uses StackExchange.Redis SCAN + DEL for production-safe prefix operations.
///   MemoryCacheAdapter tracks keys internally.
///
/// Note on GetOrCreateAsync:
///   Node-level anti-stampede via SemaphoreSlim per key.
///   For distributed lock (multi-instance), extend DistributedCacheAdapter.
/// </summary>
public interface ICache
{
    /// <summary>
    /// Get a cached value. Returns null/default if not found or expired.
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);

    /// <summary>
    /// Set a value in the cache with optional absolute expiration.
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default);

    /// <summary>
    /// Remove a specific key from the cache.
    /// </summary>
    Task RemoveAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Remove all keys matching the given prefix.
    /// MemoryCache: iterates tracked key registry.
    /// Redis: uses SCAN + DEL commands via IConnectionMultiplexer.
    /// </summary>
    Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);

    /// <summary>
    /// Get or create a cached value using the factory if not present.
    /// Protected against cache stampede via node-level locking (SemaphoreSlim).
    /// </summary>
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken ct = default);
}
