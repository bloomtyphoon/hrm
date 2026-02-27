using System.Collections.Concurrent;
using System.Text.Json;
using HRM.BuildingBlocks.Application.Abstractions.Caching;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace HRM.BuildingBlocks.Infrastructure.Caching;

/// <summary>
/// ICache implementation backed by IDistributedCache (Redis).
/// Suitable for multi-instance production deployments.
///
/// Serialization: System.Text.Json (UTF-8 bytes).
///
/// RemoveByPrefixAsync:
///   Uses IConnectionMultiplexer.GetServers() + KeysAsync (SCAN) + KeyDeleteAsync.
///   Redis instance name prefix is automatically prepended to the scan pattern.
///
/// Anti-stampede:
///   GetOrCreateAsync uses a per-key SemaphoreSlim for node-level protection.
///   For true distributed lock (multi-instance critical paths), extend with Redis SET NX.
/// </summary>
public sealed class DistributedCacheAdapter : ICache
{
    private readonly IDistributedCache _cache;
    private readonly IConnectionMultiplexer _multiplexer;
    private readonly string _instanceName;

    // Per-key semaphores for node-level stampede protection
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.Ordinal);

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public DistributedCacheAdapter(
        IDistributedCache cache,
        IConnectionMultiplexer multiplexer,
        IOptions<CacheSettings> settings)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _multiplexer = multiplexer ?? throw new ArgumentNullException(nameof(multiplexer));
        _instanceName = settings?.Value.Redis.InstanceName ?? "HRM:";
    }

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var bytes = await _cache.GetAsync(key, ct);
        if (bytes is null || bytes.Length == 0)
            return default;

        return JsonSerializer.Deserialize<T>(bytes, SerializerOptions);
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, SerializerOptions);

        var options = expiration.HasValue
            ? new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiration }
            : new DistributedCacheEntryOptions();

        await _cache.SetAsync(key, bytes, options, ct);
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken ct = default)
        => _cache.RemoveAsync(key, ct);

    /// <inheritdoc />
    public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        // Build Redis key pattern: {instanceName}{prefix}*
        // The instance name matches the prefix configured in AddStackExchangeRedisCache
        var pattern = $"{_instanceName}{prefix}*";

        var database = _multiplexer.GetDatabase();

        // Use SCAN on each server to find matching keys, then delete in batch
        foreach (var server in _multiplexer.GetServers())
        {
            if (!server.IsConnected) continue;

            var keys = new List<RedisKey>();
            await foreach (var key in server.KeysAsync(pattern: pattern).WithCancellation(ct))
            {
                keys.Add(key);
            }

            if (keys.Count > 0)
            {
                await database.KeyDeleteAsync([.. keys]);
            }
        }
    }

    /// <inheritdoc />
    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken ct = default)
    {
        // Fast path
        var existing = await GetAsync<T>(key, ct);
        if (existing is not null)
            return existing;

        // Node-level lock (prevents stampede within the same instance)
        // Note: For multi-instance deployments with expensive rebuilds,
        // replace with distributed lock via Redis SET NX.
        var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(ct);
        try
        {
            // Double-check after acquiring lock
            var afterLock = await GetAsync<T>(key, ct);
            if (afterLock is not null)
                return afterLock;

            var value = await factory();
            await SetAsync(key, value, expiration, ct);
            return value;
        }
        finally
        {
            semaphore.Release();
        }
    }
}
