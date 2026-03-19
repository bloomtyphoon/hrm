using HRM.BuildingBlocks.Domain.Abstractions.Security;

namespace HRM.BuildingBlocks.Application.Abstractions.Authorization;

/// <summary>
/// Registry for DB-driven data scope levels.
///
/// Loads all scope level definitions from [Identity].DataScopeLevels table at startup.
/// Registers them into DataScopeLevel's static registry for runtime lookup.
/// Validates that all well-known levels exist in DB.
///
/// Usage:
/// - Called at application startup (IHostedService or startup pipeline)
/// - After loading, DataScopeLevel.FromId/FromName work for all DB-defined levels
/// - New levels added to DB are available after app restart (or cache refresh)
/// </summary>
public interface IDataScopeLevelRegistry
{
    /// <summary>
    /// Load all scope levels from DB and register them.
    /// Should be called once at application startup.
    /// </summary>
    Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all registered scope levels (ordered by SortOrder).
    /// </summary>
    IReadOnlyList<DataScopeLevel> GetAll();

    /// <summary>
    /// Get all active scope levels.
    /// </summary>
    IReadOnlyList<DataScopeLevel> GetActive();
}
