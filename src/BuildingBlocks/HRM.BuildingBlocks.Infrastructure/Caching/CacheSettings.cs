namespace HRM.BuildingBlocks.Infrastructure.Caching;

/// <summary>
/// Global cache backend configuration.
/// Set in appsettings.json under "CacheSettings".
///
/// Examples:
///   Dev / Single-instance:
///     "CacheSettings": { "Provider": "Memory" }
///
///   Production / Multi-instance:
///     "CacheSettings": {
///       "Provider": "Redis",
///       "Redis": {
///         "ConnectionString": "redis:6379,password=xxx",
///         "InstanceName": "HRM:"
///       }
///     }
/// </summary>
public sealed class CacheSettings
{
    public const string SectionName = "CacheSettings";

    /// <summary>
    /// Cache backend provider. Accepted values: "Memory" (default) | "Redis".
    /// </summary>
    public string Provider { get; set; } = "Memory";

    /// <summary>
    /// Redis-specific settings. Only used when Provider = "Redis".
    /// </summary>
    public RedisCacheSettings Redis { get; set; } = new();
}

public sealed class RedisCacheSettings
{
    /// <summary>
    /// StackExchange.Redis connection string.
    /// Example: "localhost:6379" or "redis:6379,password=secret,ssl=true"
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Key prefix applied to all Redis keys to namespace this application.
    /// Defaults to "HRM:". Always ends with ":".
    /// </summary>
    public string InstanceName { get; set; } = "HRM:";
}
