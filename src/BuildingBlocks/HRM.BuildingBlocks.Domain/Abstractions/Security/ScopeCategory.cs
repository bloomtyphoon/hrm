namespace HRM.BuildingBlocks.Domain.Abstractions.Security;

/// <summary>
/// Fixed behavioral categories for data scope levels.
/// Unlike DataScopeLevel (which is dynamic/DB-driven), ScopeCategory
/// defines how the scope is RESOLVED and FILTERED — this logic is code-level.
///
/// Each category has a distinct resolution and filtering strategy:
/// - None: No access, no filtering needed
/// - Global: Full access, no filtering needed
/// - Set: Filter by OwnerId IN (resolved employee IDs)
/// - Dimension: Filter by [ScopeDimension] property IN (dimension IDs)
/// </summary>
public enum ScopeCategory
{
    /// <summary>No access — explicit deny.</summary>
    None = 0,

    /// <summary>Full access — no filtering.</summary>
    Global = 1,

    /// <summary>
    /// Set-based scope — filter by OwnerId IN (resolved employee IDs).
    /// Resolution strategy varies by level (Self, DirectReports, AllSubordinates).
    /// </summary>
    Set = 2,

    /// <summary>
    /// Dimension-based scope — filter by entity property IN (dimension IDs).
    /// The specific property is determined by the level's DimensionKey
    /// matching a [ScopeDimension] attribute on the entity.
    /// </summary>
    Dimension = 3
}
