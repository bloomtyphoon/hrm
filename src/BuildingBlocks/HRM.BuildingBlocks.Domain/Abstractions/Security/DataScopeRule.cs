namespace HRM.BuildingBlocks.Domain.Abstractions.Security;

/// <summary>
/// Immutable data scope rule — the "compiled filter instruction".
///
/// Design principles:
/// - Exactly ONE dimension/set per rule (no ambiguous multi-state)
/// - Factory methods enforce valid construction (illegal states are unrepresentable)
/// - No identity info (UserId) — only filter-relevant data
///
/// Rule types:
/// 1. Dimension-based: Company, Department, Position — filter by [ScopeDimension] property
/// 2. Set-based: Self, EmployeeSet — filter by OwnerId
///
/// Architecture layers:
///   Organization module → resolves scope logic → produces DataScopeRule
///   EfScopeExpressionBuilder → translates rule to EF Expression
///   SqlScopeWhereBuilder → translates rule to SQL WHERE clause
///
/// Usage:
/// <code>
/// var rule = await dataScopeService.GetScopeRuleAsync(userId, permission);
///
/// // EF Core
/// var expr = EfScopeExpressionBuilder.Build&lt;Employee&gt;(rule);
/// query.Where(expr);
///
/// // Dapper/SQL
/// var where = SqlScopeWhereBuilder.Build(rule, parameters);
/// </code>
/// </summary>
public sealed class DataScopeRule
{
    /// <summary>
    /// The scope level this rule filters on.
    /// </summary>
    public DataScopeLevel Level { get; }

    /// <summary>
    /// IDs for dimension-based scopes (Company, Department, Position).
    /// Empty for other levels.
    /// </summary>
    public IReadOnlyCollection<Guid> DimensionIds { get; }

    /// <summary>
    /// Employee ID for Self scope.
    /// Only set when Level == Self.
    /// </summary>
    public Guid? SelfEmployeeId { get; }

    /// <summary>
    /// Employee IDs for EmployeeSet scope (hierarchical/custom sets).
    /// Only set when Level == EmployeeSet.
    ///
    /// Examples:
    /// - Manager's subordinates (resolved by IHierarchyScopeResolver)
    /// - Custom team members
    /// - Project participants
    /// </summary>
    public IReadOnlyCollection<Guid> EmployeeIds { get; }

    /// <summary>
    /// Whether this rule grants any data access.
    /// </summary>
    public bool HasAccess => Level != DataScopeLevel.None;

    /// <summary>
    /// Whether this is a dimension-based rule (vs set-based).
    /// </summary>
    public bool IsDimensionBased => Level is DataScopeLevel.Company
        or DataScopeLevel.Department
        or DataScopeLevel.Position;

    /// <summary>
    /// Whether this is a set-based rule (Self or EmployeeSet).
    /// </summary>
    public bool IsSetBased => Level is DataScopeLevel.Self or DataScopeLevel.EmployeeSet;

    private DataScopeRule(
        DataScopeLevel level,
        IEnumerable<Guid>? dimensionIds = null,
        Guid? selfEmployeeId = null,
        IEnumerable<Guid>? employeeIds = null)
    {
        Level = level;

        // Guard and assign based on level type
        switch (level)
        {
            case DataScopeLevel.Self:
                if (selfEmployeeId is null)
                    throw new ArgumentException("Self scope requires selfEmployeeId.", nameof(selfEmployeeId));
                SelfEmployeeId = selfEmployeeId;
                DimensionIds = Array.Empty<Guid>();
                EmployeeIds = Array.Empty<Guid>();
                break;

            case DataScopeLevel.EmployeeSet:
                var empIds = employeeIds?.ToArray() ?? [];
                if (empIds.Length == 0)
                    throw new ArgumentException("EmployeeSet scope requires at least one employee ID.", nameof(employeeIds));
                EmployeeIds = empIds;
                DimensionIds = Array.Empty<Guid>();
                SelfEmployeeId = null;
                break;

            case DataScopeLevel.Company:
            case DataScopeLevel.Department:
            case DataScopeLevel.Position:
                var dimIds = dimensionIds?.ToArray() ?? [];
                if (dimIds.Length == 0)
                    throw new ArgumentException($"{level} scope requires at least one dimension ID.", nameof(dimensionIds));
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

    #region Factory Methods

    /// <summary>Global access — no filtering.</summary>
    public static DataScopeRule Global() => new(DataScopeLevel.Global);

    /// <summary>Explicit deny — no access to any data.</summary>
    public static DataScopeRule None() => new(DataScopeLevel.None);

    /// <summary>Self scope — filter to employee's own data only.</summary>
    public static DataScopeRule Self(Guid employeeId)
        => new(DataScopeLevel.Self, selfEmployeeId: employeeId);

    /// <summary>
    /// Employee set scope — filter to a custom resolved set of employees.
    ///
    /// Use for hierarchical scope (manager → subordinates) or any custom employee set.
    /// The Organization module resolves the set via IHierarchyScopeResolver.
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

    #endregion
}
