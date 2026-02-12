namespace HRM.Modules.Identity.Infrastructure.Configuration;

/// <summary>
/// Cache duration settings for Identity module services.
/// Bind to "IdentityCacheSettings" section in appsettings.json.
/// </summary>
public sealed class IdentityCacheSettings
{
    public const string SectionName = "IdentityCacheSettings";

    /// <summary>
    /// Cache duration for user permissions (in minutes).
    /// Default: 5 minutes.
    /// </summary>
    public int PermissionCacheDurationMinutes { get; set; } = 5;

    /// <summary>
    /// Cache duration for permission catalog (in minutes).
    /// Default: 60 minutes (1 hour).
    /// </summary>
    public int CatalogCacheDurationMinutes { get; set; } = 60;

    /// <summary>
    /// Cache duration for data scope rules / employee assignments (in minutes).
    /// Default: 5 minutes.
    /// </summary>
    public int DataScopeRuleCacheDurationMinutes { get; set; } = 5;
}
