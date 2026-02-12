namespace HRM.BuildingBlocks.Infrastructure.Persistence;

/// <summary>
/// Configuration options for database connection resilience.
/// Bind to "DatabaseSettings" section in appsettings.json.
/// </summary>
public sealed class DatabaseSettings
{
    public const string SectionName = "DatabaseSettings";

    /// <summary>
    /// Maximum number of retry attempts for transient database failures.
    /// Default: 3.
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// Maximum delay between retry attempts (in seconds).
    /// Default: 5.
    /// </summary>
    public int MaxRetryDelaySeconds { get; set; } = 5;

    /// <summary>
    /// SQL command timeout (in seconds).
    /// Default: 30.
    /// </summary>
    public int CommandTimeoutSeconds { get; set; } = 30;
}
