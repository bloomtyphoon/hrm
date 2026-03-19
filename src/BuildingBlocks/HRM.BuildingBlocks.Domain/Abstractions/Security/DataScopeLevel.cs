using System.Collections.Concurrent;

namespace HRM.BuildingBlocks.Domain.Abstractions.Security;

/// <summary>
/// Data scope level — defines a specific scope for data filtering.
///
/// DB-driven: scope levels are loaded from [Identity].DataScopeLevels table.
/// Well-known static instances provide compile-time references for common levels.
/// New levels can be added via DB without code changes.
///
/// Two behavioral categories (see ScopeCategory):
/// 1. Set-based: Self, DirectReports, EmployeeSet — filter by OwnerId IN (resolved IDs)
/// 2. Dimension-based: Position, Department, Company, Country, Region — filter by dimension property
///
/// Equality is by Id only. Ordering is by SortOrder.
/// </summary>
public sealed class DataScopeLevel : IEquatable<DataScopeLevel>, IComparable<DataScopeLevel>
{
    private static readonly ConcurrentDictionary<int, DataScopeLevel> _byId = new();
    private static readonly ConcurrentDictionary<string, DataScopeLevel> _byName = new(StringComparer.OrdinalIgnoreCase);

    #region Properties

    /// <summary>Unique identifier (stored as INT in DB).</summary>
    public int Id { get; }

    /// <summary>Canonical name (e.g., "Self", "Company"). Unique.</summary>
    public string Name { get; }

    /// <summary>Behavioral category: None, Global, Set, or Dimension.</summary>
    public ScopeCategory Category { get; }

    /// <summary>Sort order (narrowest to widest).</summary>
    public int SortOrder { get; }

    /// <summary>
    /// For Dimension category: the key that matches [ScopeDimension("key")] on entity properties.
    /// Null for non-dimension levels.
    /// </summary>
    public string? DimensionKey { get; }

    /// <summary>
    /// For Set category: the resolution strategy key (e.g., "Self", "DirectReports", "AllSubordinates").
    /// Used by DataScopeService to select the correct resolver.
    /// Null for non-set levels.
    /// </summary>
    public string? ResolutionKey { get; }

    #endregion

    #region Convenience Properties

    /// <summary>Whether this is the None (deny) level.</summary>
    public bool IsNone => Category == ScopeCategory.None;

    /// <summary>Whether this is the Global (full access) level.</summary>
    public bool IsGlobal => Category == ScopeCategory.Global;

    /// <summary>Whether this is a set-based level (filters by OwnerId).</summary>
    public bool IsSetBased => Category == ScopeCategory.Set;

    /// <summary>Whether this is a dimension-based level (filters by dimension property).</summary>
    public bool IsDimensionBased => Category == ScopeCategory.Dimension;

    #endregion

    #region Well-Known Instances

    /// <summary>No access — explicit deny.</summary>
    public static readonly DataScopeLevel None = Register(new(0, "None", ScopeCategory.None, 0));

    /// <summary>Self scope — user sees only their own data.</summary>
    public static readonly DataScopeLevel Self = Register(new(1, "Self", ScopeCategory.Set, 1, resolutionKey: "Self"));

    /// <summary>Direct-reports scope — self + immediate subordinates (depth = 1).</summary>
    public static readonly DataScopeLevel DirectReports = Register(new(2, "DirectReports", ScopeCategory.Set, 2, resolutionKey: "DirectReports"));

    /// <summary>Full-hierarchy scope — self + all recursive subordinates (depth = infinite).</summary>
    public static readonly DataScopeLevel EmployeeSet = Register(new(3, "EmployeeSet", ScopeCategory.Set, 3, resolutionKey: "AllSubordinates"));

    /// <summary>Position scope — filter by position dimension.</summary>
    public static readonly DataScopeLevel Position = Register(new(4, "Position", ScopeCategory.Dimension, 4, dimensionKey: "Position"));

    /// <summary>Department scope — filter by department dimension.</summary>
    public static readonly DataScopeLevel Department = Register(new(5, "Department", ScopeCategory.Dimension, 5, dimensionKey: "Department"));

    /// <summary>Company scope — filter by company dimension.</summary>
    public static readonly DataScopeLevel Company = Register(new(6, "Company", ScopeCategory.Dimension, 6, dimensionKey: "Company"));

    /// <summary>Country scope — filter by country dimension.</summary>
    public static readonly DataScopeLevel Country = Register(new(7, "Country", ScopeCategory.Dimension, 7, dimensionKey: "Country"));

    /// <summary>Region scope — filter by geographic region dimension.</summary>
    public static readonly DataScopeLevel Region = Register(new(8, "Region", ScopeCategory.Dimension, 8, dimensionKey: "Region"));

    /// <summary>Global scope — no filtering, full tenant access.</summary>
    public static readonly DataScopeLevel Global = Register(new(9, "Global", ScopeCategory.Global, 9));

    #endregion

    #region Constructor

    private DataScopeLevel(
        int id, string name, ScopeCategory category, int sortOrder,
        string? dimensionKey = null, string? resolutionKey = null)
    {
        Id = id;
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Category = category;
        SortOrder = sortOrder;
        DimensionKey = dimensionKey;
        ResolutionKey = resolutionKey;
    }

    #endregion

    #region Registry

    /// <summary>
    /// Register a scope level (called at startup from DB-loaded definitions).
    /// Overwrites existing registration with same Id.
    /// </summary>
    public static DataScopeLevel Register(int id, string name, ScopeCategory category, int sortOrder,
        string? dimensionKey = null, string? resolutionKey = null)
    {
        var level = new DataScopeLevel(id, name, category, sortOrder, dimensionKey, resolutionKey);
        return Register(level);
    }

    private static DataScopeLevel Register(DataScopeLevel level)
    {
        _byId[level.Id] = level;
        _byName[level.Name] = level;
        return level;
    }

    /// <summary>
    /// Get a scope level by its integer ID.
    /// Returns the registered level, or throws if not found.
    /// </summary>
    public static DataScopeLevel FromId(int id)
    {
        if (_byId.TryGetValue(id, out var level))
            return level;

        throw new ArgumentOutOfRangeException(nameof(id),
            $"Unknown DataScopeLevel ID: {id}. Ensure all scope levels are registered from DB.");
    }

    /// <summary>
    /// Try to get a scope level by its integer ID.
    /// Returns null if not found.
    /// </summary>
    public static DataScopeLevel? TryFromId(int id)
    {
        return _byId.TryGetValue(id, out var level) ? level : null;
    }

    /// <summary>
    /// Get a scope level by its canonical name.
    /// </summary>
    public static DataScopeLevel FromName(string name)
    {
        if (_byName.TryGetValue(name, out var level))
            return level;

        throw new ArgumentException(
            $"Unknown DataScopeLevel name: '{name}'. Ensure all scope levels are registered from DB.",
            nameof(name));
    }

    /// <summary>
    /// Try to get a scope level by name. Returns null if not found.
    /// </summary>
    public static DataScopeLevel? TryFromName(string name)
    {
        return _byName.TryGetValue(name, out var level) ? level : null;
    }

    /// <summary>
    /// Get all registered scope levels, ordered by SortOrder.
    /// </summary>
    public static IReadOnlyList<DataScopeLevel> All
        => _byId.Values.OrderBy(l => l.SortOrder).ToList();

    /// <summary>
    /// Get all registered scope levels of a specific category.
    /// </summary>
    public static IReadOnlyList<DataScopeLevel> GetByCategory(ScopeCategory category)
        => _byId.Values.Where(l => l.Category == category).OrderBy(l => l.SortOrder).ToList();

    #endregion

    #region Equality & Comparison

    public bool Equals(DataScopeLevel? other) => other is not null && Id == other.Id;
    public override bool Equals(object? obj) => Equals(obj as DataScopeLevel);
    public override int GetHashCode() => Id;

    public static bool operator ==(DataScopeLevel? left, DataScopeLevel? right)
    {
        if (left is null) return right is null;
        return left.Equals(right);
    }

    public static bool operator !=(DataScopeLevel? left, DataScopeLevel? right) => !(left == right);

    public int CompareTo(DataScopeLevel? other)
    {
        if (other is null) return 1;
        return SortOrder.CompareTo(other.SortOrder);
    }

    public static bool operator >(DataScopeLevel left, DataScopeLevel right) => left.SortOrder > right.SortOrder;
    public static bool operator <(DataScopeLevel left, DataScopeLevel right) => left.SortOrder < right.SortOrder;
    public static bool operator >=(DataScopeLevel left, DataScopeLevel right) => left.SortOrder >= right.SortOrder;
    public static bool operator <=(DataScopeLevel left, DataScopeLevel right) => left.SortOrder <= right.SortOrder;

    /// <summary>Implicit conversion to int for backward compatibility with DB storage.</summary>
    public static implicit operator int(DataScopeLevel level) => level.Id;

    #endregion

    public override string ToString() => Name;
}
