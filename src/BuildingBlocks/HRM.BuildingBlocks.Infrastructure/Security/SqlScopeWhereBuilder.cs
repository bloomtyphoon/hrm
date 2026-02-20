using Dapper;
using HRM.BuildingBlocks.Domain.Abstractions.Security;

namespace HRM.BuildingBlocks.Infrastructure.Security;

/// <summary>
/// Translates DataScopeRule/DataScopePolicy to SQL WHERE clause.
///
/// NOTE: Unlike EfScopeExpressionBuilder, SQL builder requires explicit column mapping.
/// This is OK because the module using the builder provides the mapping,
/// BB does NOT define the org structure.
///
/// Usage:
/// <code>
/// // Single rule
/// var rule = DataScopeRule.Department([D1]);
/// var parameters = new DynamicParameters();
/// var where = SqlScopeWhereBuilder.Build(rule, parameters);
/// var sql = $"SELECT * FROM Employees e WHERE 1=1 {where}";
///
/// // Policy
/// var policy = DataScopePolicy.Or(
///     DataScopeRule.Department([D1]),
///     DataScopeRule.Position([P9])
/// );
/// var where = SqlScopeWhereBuilder.Build(policy, parameters);
/// </code>
/// </summary>
public static class SqlScopeWhereBuilder
{
    #region Single Rule API

    /// <summary>
    /// Build SQL WHERE clause fragment with standard column names.
    /// </summary>
    public static string Build(DataScopeRule rule, DynamicParameters parameters)
    {
        return Build(rule, parameters, new SqlScopeColumnMapping());
    }

    /// <summary>
    /// Build SQL WHERE clause with custom column mapping.
    /// </summary>
    public static string Build(DataScopeRule rule, DynamicParameters parameters, SqlScopeColumnMapping columns)
    {
        return rule.Level switch
        {
            DataScopeLevel.Global => string.Empty,
            DataScopeLevel.None => "AND 1 = 0",

            // Set-based scopes
            DataScopeLevel.Self when rule.SelfEmployeeId.HasValue =>
                BuildSelfFilter(rule.SelfEmployeeId.Value, parameters, columns.OwnerColumn),

            DataScopeLevel.EmployeeSet when rule.EmployeeIds.Count > 0 =>
                BuildEmployeeSetFilter(rule.EmployeeIds, parameters, columns.OwnerColumn),

            // Dimension-based scopes
            DataScopeLevel.Position =>
                BuildDimensionFilter(rule.DimensionIds, parameters, columns.PositionColumn, "@ScopePositionIds"),

            DataScopeLevel.Department =>
                BuildDimensionFilter(rule.DimensionIds, parameters, columns.DepartmentColumn, "@ScopeDepartmentIds"),

            DataScopeLevel.Company =>
                BuildDimensionFilter(rule.DimensionIds, parameters, columns.CompanyColumn, "@ScopeCompanyIds"),

            _ => "AND 1 = 0"
        };
    }

    #endregion

    #region Policy API

    /// <summary>
    /// Build SQL WHERE clause from a policy (multiple rules combined).
    /// </summary>
    public static string Build(DataScopePolicy policy, DynamicParameters parameters)
    {
        return Build(policy, parameters, new SqlScopeColumnMapping());
    }

    /// <summary>
    /// Build SQL WHERE clause from a policy with custom column mapping.
    /// </summary>
    public static string Build(DataScopePolicy policy, DynamicParameters parameters, SqlScopeColumnMapping columns)
    {
        var simplified = policy.Simplify();

        // Single rule - direct build
        if (simplified.IsSingleRule)
            return Build(simplified.SingleRule!, parameters, columns);

        // Multiple rules - combine
        var clauses = new List<string>();
        var counter = 0;

        foreach (var rule in simplified.Rules)
        {
            var clause = BuildRuleClause(rule, parameters, columns, counter++);
            if (!string.IsNullOrEmpty(clause))
                clauses.Add(clause);
        }

        if (clauses.Count == 0)
            return simplified.Combinator == PolicyCombinator.Or ? "AND 1 = 0" : string.Empty;

        var combined = string.Join(
            simplified.Combinator == PolicyCombinator.Or ? " OR " : " AND ",
            clauses);

        return $"AND ({combined})";
    }

    /// <summary>
    /// Build standalone WHERE clause from policy (without leading AND).
    /// </summary>
    public static string BuildStandalone(DataScopePolicy policy, DynamicParameters parameters)
    {
        return BuildStandalone(policy, parameters, new SqlScopeColumnMapping());
    }

    /// <summary>
    /// Build standalone WHERE clause from policy with custom columns.
    /// </summary>
    public static string BuildStandalone(DataScopePolicy policy, DynamicParameters parameters, SqlScopeColumnMapping columns)
    {
        var simplified = policy.Simplify();

        if (simplified.IsSingleRule)
            return BuildStandalone(simplified.SingleRule!, parameters, columns);

        var clauses = new List<string>();
        var counter = 0;

        foreach (var rule in simplified.Rules)
        {
            var clause = BuildRuleClauseStandalone(rule, parameters, columns, counter++);
            if (!string.IsNullOrEmpty(clause))
                clauses.Add(clause);
        }

        if (clauses.Count == 0)
            return simplified.Combinator == PolicyCombinator.Or ? "1 = 0" : "1 = 1";

        var combined = string.Join(
            simplified.Combinator == PolicyCombinator.Or ? " OR " : " AND ",
            clauses);

        return clauses.Count > 1 ? $"({combined})" : combined;
    }

    #endregion

    #region Legacy Single Rule API (backward compatible)

    /// <summary>
    /// Build for employee-based queries with join to EmployeeAssignments.
    /// </summary>
    public static string BuildWithAssignments(
        DataScopeRule rule,
        DynamicParameters parameters,
        string employeeAlias = "e",
        string assignmentAlias = "ea")
    {
        return rule.Level switch
        {
            DataScopeLevel.Global => string.Empty,
            DataScopeLevel.None => "AND 1 = 0",

            DataScopeLevel.Self when rule.SelfEmployeeId.HasValue =>
                BuildSelfFilter(rule.SelfEmployeeId.Value, parameters, $"{employeeAlias}.Id", "@ScopeEmployeeId"),

            DataScopeLevel.EmployeeSet when rule.EmployeeIds.Count > 0 =>
                BuildEmployeeSetFilter(rule.EmployeeIds, parameters, $"{employeeAlias}.Id"),

            DataScopeLevel.Position =>
                BuildDimensionFilter(rule.DimensionIds, parameters, $"{assignmentAlias}.PositionId", "@ScopePositionIds"),

            DataScopeLevel.Department =>
                BuildDimensionFilter(rule.DimensionIds, parameters, $"{assignmentAlias}.DepartmentId", "@ScopeDepartmentIds"),

            DataScopeLevel.Company =>
                BuildDimensionFilter(rule.DimensionIds, parameters, $"{assignmentAlias}.CompanyId", "@ScopeCompanyIds"),

            _ => "AND 1 = 0"
        };
    }

    /// <summary>
    /// Build standalone WHERE clause (without leading AND).
    /// </summary>
    public static string BuildStandalone(DataScopeRule rule, DynamicParameters parameters)
    {
        return BuildStandalone(rule, parameters, new SqlScopeColumnMapping());
    }

    /// <summary>
    /// Build standalone WHERE clause with custom column mapping.
    /// </summary>
    public static string BuildStandalone(DataScopeRule rule, DynamicParameters parameters, SqlScopeColumnMapping columns)
    {
        return rule.Level switch
        {
            DataScopeLevel.Global => "1 = 1",
            DataScopeLevel.None => "1 = 0",

            DataScopeLevel.Self when rule.SelfEmployeeId.HasValue =>
                BuildStandaloneSelf(rule.SelfEmployeeId.Value, parameters, columns.OwnerColumn),

            DataScopeLevel.EmployeeSet when rule.EmployeeIds.Count > 0 =>
                BuildStandaloneEmployeeSet(rule.EmployeeIds, parameters, columns.OwnerColumn),

            DataScopeLevel.Position =>
                BuildStandaloneDimension(rule.DimensionIds, parameters, columns.PositionColumn, "@ScopePositionIds"),

            DataScopeLevel.Department =>
                BuildStandaloneDimension(rule.DimensionIds, parameters, columns.DepartmentColumn, "@ScopeDepartmentIds"),

            DataScopeLevel.Company =>
                BuildStandaloneDimension(rule.DimensionIds, parameters, columns.CompanyColumn, "@ScopeCompanyIds"),

            _ => "1 = 0"
        };
    }

    #endregion

    #region Private Helpers

    private static string BuildRuleClause(
        DataScopeRule rule, DynamicParameters parameters, SqlScopeColumnMapping columns, int counter)
    {
        var suffix = counter > 0 ? $"_{counter}" : "";

        return rule.Level switch
        {
            DataScopeLevel.Global => "1 = 1",
            DataScopeLevel.None => "1 = 0",

            DataScopeLevel.Self when rule.SelfEmployeeId.HasValue =>
                BuildSelfClause(rule.SelfEmployeeId.Value, parameters, columns.OwnerColumn, $"@ScopeEmployeeId{suffix}"),

            DataScopeLevel.EmployeeSet when rule.EmployeeIds.Count > 0 =>
                BuildEmployeeSetClause(rule.EmployeeIds, parameters, columns.OwnerColumn, $"@ScopeEmployeeIds{suffix}"),

            DataScopeLevel.Position =>
                BuildDimensionClause(rule.DimensionIds, parameters, columns.PositionColumn, $"@ScopePositionIds{suffix}"),

            DataScopeLevel.Department =>
                BuildDimensionClause(rule.DimensionIds, parameters, columns.DepartmentColumn, $"@ScopeDepartmentIds{suffix}"),

            DataScopeLevel.Company =>
                BuildDimensionClause(rule.DimensionIds, parameters, columns.CompanyColumn, $"@ScopeCompanyIds{suffix}"),

            _ => "1 = 0"
        };
    }

    private static string BuildRuleClauseStandalone(
        DataScopeRule rule, DynamicParameters parameters, SqlScopeColumnMapping columns, int counter)
    {
        // Same as BuildRuleClause but for standalone use
        return BuildRuleClause(rule, parameters, columns, counter);
    }

    private static string BuildDimensionFilter(
        IReadOnlyCollection<Guid> ids, DynamicParameters parameters,
        string column, string paramName)
    {
        parameters.Add(paramName, ids);
        return $"AND {column} IN {paramName}";
    }

    private static string BuildDimensionClause(
        IReadOnlyCollection<Guid> ids, DynamicParameters parameters,
        string column, string paramName)
    {
        parameters.Add(paramName, ids);
        return $"{column} IN {paramName}";
    }

    private static string BuildSelfFilter(
        Guid employeeId, DynamicParameters parameters,
        string column, string paramName = "@ScopeEmployeeId")
    {
        parameters.Add(paramName, employeeId);
        return $"AND {column} = {paramName}";
    }

    private static string BuildSelfClause(
        Guid employeeId, DynamicParameters parameters,
        string column, string paramName)
    {
        parameters.Add(paramName, employeeId);
        return $"{column} = {paramName}";
    }

    private static string BuildEmployeeSetFilter(
        IReadOnlyCollection<Guid> employeeIds, DynamicParameters parameters,
        string column, string paramName = "@ScopeEmployeeIds")
    {
        parameters.Add(paramName, employeeIds);
        return $"AND {column} IN {paramName}";
    }

    private static string BuildEmployeeSetClause(
        IReadOnlyCollection<Guid> employeeIds, DynamicParameters parameters,
        string column, string paramName)
    {
        parameters.Add(paramName, employeeIds);
        return $"{column} IN {paramName}";
    }

    private static string BuildStandaloneDimension(
        IReadOnlyCollection<Guid> ids, DynamicParameters parameters,
        string column, string paramName)
    {
        parameters.Add(paramName, ids);
        return $"{column} IN {paramName}";
    }

    private static string BuildStandaloneSelf(
        Guid employeeId, DynamicParameters parameters, string column)
    {
        parameters.Add("@ScopeEmployeeId", employeeId);
        return $"{column} = @ScopeEmployeeId";
    }

    private static string BuildStandaloneEmployeeSet(
        IReadOnlyCollection<Guid> employeeIds, DynamicParameters parameters, string column)
    {
        parameters.Add("@ScopeEmployeeIds", employeeIds);
        return $"{column} IN @ScopeEmployeeIds";
    }

    #endregion
}

/// <summary>
/// Column name mapping for SQL scope queries.
/// Override defaults when table uses different column names.
///
/// NOTE: This is NOT org structure leak - the module using SQL builder
/// provides its own column mapping. BB does not define what columns exist.
/// </summary>
public sealed class SqlScopeColumnMapping
{
    public string CompanyColumn { get; init; } = "CompanyId";
    public string DepartmentColumn { get; init; } = "DepartmentId";
    public string PositionColumn { get; init; } = "PositionId";
    public string OwnerColumn { get; init; } = "OwnerId";
    public string EmployeeColumn { get; init; } = "EmployeeId";
}
