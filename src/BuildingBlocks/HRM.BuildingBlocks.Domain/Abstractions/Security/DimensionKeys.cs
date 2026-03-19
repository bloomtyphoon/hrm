namespace HRM.BuildingBlocks.Domain.Abstractions.Security;

/// <summary>
/// Well-known dimension keys used in [ScopeDimension] attributes and DataScopeLevel definitions.
///
/// Single source of truth: both entity attributes and DB-defined scope levels
/// must use these constants to ensure consistency.
///
/// Adding a new dimension:
/// 1. Add a constant here
/// 2. Add [ScopeDimension(DimensionKeys.X)] to the entity property
/// 3. Add a DataScopeLevel row in DB with DimensionKey = DimensionKeys.X
/// </summary>
public static class DimensionKeys
{
    public const string Company = "Company";
    public const string Department = "Department";
    public const string Position = "Position";
    public const string Country = "Country";
    public const string Region = "Region";

    /// <summary>All well-known dimension keys.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        Company,
        Department,
        Position,
        Country,
        Region
    ];

    /// <summary>Check if a dimension key is well-known.</summary>
    public static bool IsKnown(string key)
        => All.Contains(key, StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Well-known resolution keys for set-based scope levels.
///
/// Used by DataScopeService to select the correct resolver
/// for set-based scopes (Self, DirectReports, AllSubordinates).
/// </summary>
public static class ResolutionKeys
{
    public const string Self = "Self";
    public const string DirectReports = "DirectReports";
    public const string AllSubordinates = "AllSubordinates";

    /// <summary>All well-known resolution keys.</summary>
    public static IReadOnlyList<string> All { get; } =
    [
        Self,
        DirectReports,
        AllSubordinates
    ];

    /// <summary>Check if a resolution key is well-known.</summary>
    public static bool IsKnown(string key)
        => All.Contains(key, StringComparer.OrdinalIgnoreCase);
}
