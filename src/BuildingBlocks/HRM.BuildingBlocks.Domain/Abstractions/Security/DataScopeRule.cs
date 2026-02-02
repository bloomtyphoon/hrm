namespace HRM.BuildingBlocks.Domain.Abstractions.Security;

/// <summary>
/// Immutable, level-based data scope rule — the "compiled filter instruction".
///
/// Design principles:
/// - Exactly ONE dimension per rule (no ambiguous multi-boolean states)
/// - Hierarchy encoded in DataScopeLevel enum (enables >= comparisons)
/// - Factory methods enforce valid construction (illegal states are unrepresentable)
/// - No identity info (UserId) — only filter-relevant data
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
    /// The scope level (dimension) this rule filters on.
    /// </summary>
    public DataScopeLevel Level { get; }

    /// <summary>
    /// IDs for the current dimension (CompanyIds, DepartmentIds, or PositionIds).
    /// Empty for Global, None, and Self levels.
    /// </summary>
    public IReadOnlyCollection<Guid> DimensionIds { get; }

    /// <summary>
    /// Employee ID for Self scope filtering.
    /// Only set when Level == Self.
    /// </summary>
    public Guid? SelfEmployeeId { get; }

    /// <summary>
    /// Whether this rule grants any data access.
    /// </summary>
    public bool HasAccess => Level != DataScopeLevel.None;

    private DataScopeRule(
        DataScopeLevel level,
        IEnumerable<Guid>? dimensionIds = null,
        Guid? selfEmployeeId = null)
    {
        // Guard: Self requires employeeId
        if (level == DataScopeLevel.Self && selfEmployeeId is null)
            throw new ArgumentException("Self scope requires selfEmployeeId.", nameof(selfEmployeeId));

        // Guard: Company/Department/Position require at least one ID
        if (level is DataScopeLevel.Company or DataScopeLevel.Department or DataScopeLevel.Position)
        {
            var ids = dimensionIds?.ToArray() ?? [];
            if (ids.Length == 0)
                throw new ArgumentException($"{level} scope requires at least one dimension ID.", nameof(dimensionIds));
            DimensionIds = ids;
        }
        else
        {
            DimensionIds = Array.Empty<Guid>();
        }

        Level = level;
        SelfEmployeeId = selfEmployeeId;
    }

    /// <summary>Global access — no filtering.</summary>
    public static DataScopeRule Global() => new(DataScopeLevel.Global);

    /// <summary>Explicit deny — no access to any data.</summary>
    public static DataScopeRule None() => new(DataScopeLevel.None);

    /// <summary>Company scope — filter by company IDs.</summary>
    public static DataScopeRule Company(IEnumerable<Guid> companyIds)
        => new(DataScopeLevel.Company, companyIds);

    /// <summary>Department scope — filter by department IDs.</summary>
    public static DataScopeRule Department(IEnumerable<Guid> departmentIds)
        => new(DataScopeLevel.Department, departmentIds);

    /// <summary>Position scope — filter by position IDs.</summary>
    public static DataScopeRule Position(IEnumerable<Guid> positionIds)
        => new(DataScopeLevel.Position, positionIds);

    /// <summary>Self scope — filter to employee's own data only.</summary>
    public static DataScopeRule Self(Guid employeeId)
        => new(DataScopeLevel.Self, selfEmployeeId: employeeId);
}
