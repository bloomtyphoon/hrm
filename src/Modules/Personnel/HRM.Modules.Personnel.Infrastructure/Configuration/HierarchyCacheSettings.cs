namespace HRM.Modules.Personnel.Infrastructure.Configuration;

/// <summary>
/// Configurable TTL settings for employee hierarchy caching.
///
/// TTL strategy by org size (based on cache-thrashing analysis):
///
///   Small  (< 1k employees)   → 30 min  – stable hierarchy, aggressive caching
///   Medium (1k–10k employees) → 10 min  – balanced
///   Large  (> 10k employees)  → event-driven invalidation only (TTL disabled)
///
/// For large orgs, ManagerChangedDomainEventHandler already handles invalidation
/// so TTL is just a safety net — set LargeOrgTtlMinutes = 0 to disable.
/// </summary>
public sealed class HierarchyCacheSettings
{
    public const string SectionName = "HierarchyCacheSettings";

    /// <summary>Threshold (employee count) considered a small org. Default: 1000.</summary>
    public int SmallOrgThreshold { get; set; } = 1_000;

    /// <summary>Threshold (employee count) considered a large org. Default: 10000.</summary>
    public int LargeOrgThreshold { get; set; } = 10_000;

    /// <summary>Cache TTL in minutes for small orgs (< SmallOrgThreshold). Default: 30.</summary>
    public int SmallOrgTtlMinutes { get; set; } = 30;

    /// <summary>Cache TTL in minutes for medium orgs. Default: 10.</summary>
    public int MediumOrgTtlMinutes { get; set; } = 10;

    /// <summary>
    /// Cache TTL in minutes for large orgs (> LargeOrgThreshold).
    /// Set to 0 to rely solely on event-driven invalidation (recommended for > 10k employees).
    /// Default: 5.
    /// </summary>
    public int LargeOrgTtlMinutes { get; set; } = 5;

    /// <summary>
    /// Returns the appropriate TTL for the given employee count.
    /// Returns null if TTL should be disabled (event-driven only).
    /// </summary>
    public TimeSpan? GetTtl(int employeeCount)
    {
        if (employeeCount < SmallOrgThreshold)
            return TimeSpan.FromMinutes(SmallOrgTtlMinutes);

        if (employeeCount < LargeOrgThreshold)
            return TimeSpan.FromMinutes(MediumOrgTtlMinutes);

        return LargeOrgTtlMinutes > 0
            ? TimeSpan.FromMinutes(LargeOrgTtlMinutes)
            : null;
    }
}
