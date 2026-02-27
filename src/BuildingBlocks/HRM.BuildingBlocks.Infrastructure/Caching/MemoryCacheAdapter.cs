using System.Collections.Concurrent;
using HRM.BuildingBlocks.Application.Abstractions.Caching;
using Microsoft.Extensions.Caching.Memory;

namespace HRM.BuildingBlocks.Infrastructure.Caching;

/// <summary>
/// ICache implementation backed by IMemoryCache.
/// Suitable for single-instance deployments and development.
///
/// Key tracking:
///   Maintains a ConcurrentDictionary of active keys to support RemoveByPrefixAsync,
///   since IMemoryCache does not expose key enumeration natively.
///
/// Anti-stampede:
///   GetOrCreateAsync uses a per-key SemaphoreSlim to prevent concurrent factory calls
///   from running simultaneously (node-level protection).
/// </summary>
public sealed class MemoryCacheAdapter : ICache
{
    private readonly IMemoryCache _cache;

    // Tracks all active keys for RemoveByPrefixAsync support
    private readonly ConcurrentDictionary<string, byte> _keys = new(StringComparer.Ordinal);

    // Per-key semaphores for GetOrCreateAsync stampede protection
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.Ordinal);

    public MemoryCacheAdapter(IMemoryCache cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    /// <inheritdoc />
    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        _cache.TryGetValue<T>(key, out var value);
        return Task.FromResult(value);
    }

    /// <inheritdoc />
    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken ct = default)
    {
        _keys[key] = 0;

        if (expiration.HasValue)
            _cache.Set(key, value, expiration.Value);
        else
            _cache.Set(key, value);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        _keys.TryRemove(key, out _);
        _cache.Remove(key);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        var keysToRemove = _keys.Keys
            .Where(k => k.StartsWith(prefix, StringComparison.Ordinal))
            .ToList();

        foreach (var key in keysToRemove)
        {
            _keys.TryRemove(key, out _);
            _cache.Remove(key);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken ct = default)
    {
        // Fast path — value already cached
        if (_cache.TryGetValue<T>(key, out var cached) && cached is not null)
            return cached;

        // Acquire per-key lock to prevent concurrent factory invocations (stampede protection)
        var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(ct);
        try
        {
            // Double-check after acquiring lock
            if (_cache.TryGetValue<T>(key, out var afterLock) && afterLock is not null)
                return afterLock;

            var value = await factory();

            _keys[key] = 0;
            if (expiration.HasValue)
                _cache.Set(key, value, expiration.Value);
            else
                _cache.Set(key, value);

            return value;
        }
        finally
        {
            semaphore.Release();
        }
    }
}
