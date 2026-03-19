namespace HRM.BuildingBlocks.Domain.Abstractions.Security;

/// <summary>
/// Immutable data scope rule — the "compiled filter instruction".
///
/// Design principles:
/// - Exactly ONE dimension/set per rule (no ambiguous multi-state)
/// - Factory methods enforce valid construction (illegal states are unrepresentable)
/// - No identity info (UserId) — only filter-relevant data
/// - Category-based dispatch: switch on Level.Category, not specific levels
///
/// Rule types:
/// 1. Set-based: Self, DirectReports, EmployeeSet — filter by OwnerId IN (ids)
/// 2. Dimension-based: Position, Department, Company, Country, Region
///    — filter by [ScopeDimension] property IN (DimensionIds)
///
/// Architecture layers:
///   DataScopeService → resolves scope logic → produces DataScopeRule
///   EfScopeExpressionBuilder → translates rule to EF Expression
///   SqlScopeWhereBuilder → translates rule to SQL WHERE clause
/// </summary>
public sealed class DataScopeRule
{
    /// <summary>The scope level this rule filters on.</summary>
    public DataScopeLevel Level { get; }

    /// <summary>
    /// IDs for dimension-based scopes.
    /// Empty for non-dimension levels.
    /// </summary>
    public IReadOnlyCollection<Guid> DimensionIds { get; }

    /// <summary>Employee ID for Self scope. Only set when Level == Self.</summary>
    public Guid? SelfEmployeeId { get; }

    /// <summary>
    /// Employee IDs for set-based scopes (DirectReports, EmployeeSet).
    /// DirectReports: self + immediate subordinates.
    /// EmployeeSet: self + all recursive subordinates.
    /// </summary>
    public IReadOnlyCollection<Guid> EmployeeIds { get; }

    /// <summary>Whether this rule grants any data access.</summary>
    public bool HasAccess => !Level.IsNone;

    /// <summary>Whether this is a dimension-based rule.</summary>
    public bool IsDimensionBased => Level.IsDimensionBased;

    /// <summary>Whether this is a set-based rule (filters by OwnerId).</summary>
    public bool IsSetBased => Level.IsSetBased;

    private DataScopeRule(
        DataScopeLevel level,
        IEnumerable<Guid>? dimensionIds = null,
        Guid? selfEmployeeId = null,
        IEnumerable<Guid>? employeeIds = null)
    {
        Level = level;

        switch (level.Category)
        {
            case ScopeCategory.Set when level == DataScopeLevel.Self:
                if (selfEmployeeId is null)
                    throw new ArgumentException("Self scope requires selfEmployeeId.", nameof(selfEmployeeId));
                SelfEmployeeId = selfEmployeeId;
                DimensionIds = Array.Empty<Guid>();
                EmployeeIds = Array.Empty<Guid>();
                break;

            case ScopeCategory.Set:
                var empIds = employeeIds?.ToArray() ?? [];
                if (empIds.Length == 0)
                    throw new ArgumentException($"{level.Name} scope requires at least one employee ID.", nameof(employeeIds));
                EmployeeIds = empIds;
                DimensionIds = Array.Empty<Guid>();
                SelfEmployeeId = null;
                break;

            case ScopeCategory.Dimension:
                var dimIds = dimensionIds?.ToArray() ?? [];
                if (dimIds.Length == 0)
                    throw new ArgumentException($"{level.Name} scope requires at least one dimension ID.", nameof(dimensionIds));
                DimensionIds = dimIds;
                EmployeeIds = Array.Empty<Guid>();
                SelfEmployeeId = null;
                break;

            default: // None, Global
                DimensionIds = Array.Empty<Guid>();
                EmployeeIds = Array.Empty<Guid>();
                SelfEmployeeId = null;
                break;
        }
    }

    #region Generic Factory Methods

    /// <summary>Global access — no filtering.</summary>
    public static DataScopeRule Global() => new(DataScopeLevel.Global);

    /// <summary>Explicit deny — no access to any data.</summary>
    public static DataScopeRule None() => new(DataScopeLevel.None);

    /// <summary>Self scope — filter to employee's own data only.</summary>
    public static DataScopeRule Self(Guid employeeId)
        => new(DataScopeLevel.Self, selfEmployeeId: employeeId);

    /// <summary>
    /// Direct-reports scope — filter to self + immediate subordinates only (depth = 1).
    /// </summary>
    public static DataScopeRule DirectReports(IEnumerable<Guid> employeeIds)
        => new(DataScopeLevel.DirectReports, employeeIds: employeeIds);

    /// <summary>
    /// Full-hierarchy scope — filter to self + all recursive subordinates (depth = infinite).
    /// </summary>
    public static DataScopeRule EmployeeSet(IEnumerable<Guid> employeeIds)
        => new(DataScopeLevel.EmployeeSet, employeeIds: employeeIds);

    /// <summary>Position scope — filter by position dimension.</summary>
    public static DataScopeRule Position(IEnumerable<Guid> positionIds)
        => new(DataScopeLevel.Position, positionIds);

    /// <summary>Department scope — filter by department dimension.</summary>
    public static DataScopeRule Department(IEnumerable<Guid> departmentIds)
        => new(DataScopeLevel.Department, departmentIds);

    /// <summary>Company scope — filter by company dimension.</summary>
    public static DataScopeRule Company(IEnumerable<Guid> companyIds)
        => new(DataScopeLevel.Company, companyIds);

    /// <summary>Country scope — filter by country dimension.</summary>
    public static DataScopeRule Country(IEnumerable<Guid> countryIds)
        => new(DataScopeLevel.Country, countryIds);

    /// <summary>Region scope — filter by geographic region dimension.</summary>
    public static DataScopeRule Region(IEnumerable<Guid> regionIds)
        => new(DataScopeLevel.Region, regionIds);

    /// <summary>
    /// Create a set-based scope rule for any set-based level.
    /// Use for dynamically-defined set scopes loaded from DB.
    /// </summary>
    public static DataScopeRule ForSet(DataScopeLevel level, IEnumerable<Guid> employeeIds, Guid? selfEmployeeId = null)
    {
        if (!level.IsSetBased)
            throw new ArgumentException($"Level '{level.Name}' is not set-based.", nameof(level));

        if (level == DataScopeLevel.Self && selfEmployeeId.HasValue)
            return new DataScopeRule(level, selfEmployeeId: selfEmployeeId);

        return new DataScopeRule(level, employeeIds: employeeIds);
    }

    /// <summary>
    /// Create a dimension-based scope rule for any dimension level.
    /// Use for dynamically-defined dimension scopes loaded from DB.
    /// </summary>
    public static DataScopeRule ForDimension(DataScopeLevel level, IEnumerable<Guid> dimensionIds)
    {
        if (!level.IsDimensionBased)
            throw new ArgumentException($"Level '{level.Name}' is not dimension-based.", nameof(level));

        return new DataScopeRule(level, dimensionIds);
    }

    #endregion
}
