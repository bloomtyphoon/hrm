namespace HRM.BuildingBlocks.Infrastructure.BackgroundServices;

/// <summary>
/// Configuration options for OutboxProcessor.
/// Bind to "OutboxSettings" section in appsettings.json.
/// </summary>
public sealed class OutboxSettings
{
    public const string SectionName = "OutboxSettings";

    /// <summary>
    /// How often to poll for unprocessed outbox messages (in seconds).
    /// Default: 60 seconds.
    /// </summary>
    public int PollingIntervalSeconds { get; set; } = 60;

    /// <summary>
    /// Maximum number of messages to process per polling iteration.
    /// Default: 100.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Maximum retry attempts before a message becomes dead letter.
    /// Default: 5.
    /// </summary>
    public int MaxAttempts { get; set; } = 5;
}
