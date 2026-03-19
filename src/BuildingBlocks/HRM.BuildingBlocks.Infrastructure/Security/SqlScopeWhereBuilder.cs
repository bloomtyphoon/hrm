using Dapper;
using HRM.BuildingBlocks.Domain.Abstractions.Security;

namespace HRM.BuildingBlocks.Infrastructure.Security;

/// <summary>
/// Translates DataScopeRule/DataScopePolicy to SQL WHERE clause.
///
/// Uses category-based dispatch for dynamic scope level support.
/// Column mapping is provided by SqlScopeColumnMapping (configurable per query).
/// </summary>
public static class SqlScopeWhereBuilder
{
    #region Single Rule API

    /// <summary>Build SQL WHERE clause fragment with standard column names.</summary>
    public static string Build(DataScopeRule rule, DynamicParameters parameters)
        => Build(rule, parameters, new SqlScopeColumnMapping());

    /// <summary>Build SQL WHERE clause with custom column mapping.</summary>
    public static string Build(DataScopeRule rule, DynamicParameters parameters, SqlScopeColumnMapping columns)
    {
        return rule.Level.Category switch
        {
            ScopeCategory.Global => string.Empty,
            ScopeCategory.None => "AND 1 = 0",
            ScopeCategory.Set when rule.Level == DataScopeLevel.Self && rule.SelfEmployeeId.HasValue =>
                BuildSelfFilter(rule.SelfEmployeeId.Value, parameters, columns.OwnerColumn),
            ScopeCategory.Set when rule.EmployeeIds.Count > 0 =>
                BuildEmployeeSetFilter(rule.EmployeeIds, parameters, columns.OwnerColumn),
            ScopeCategory.Dimension when rule.Level.DimensionKey is not null =>
                BuildDimensionFilter(rule.DimensionIds, parameters,
                    columns.GetColumn(rule.Level.DimensionKey),
                    $"@Scope{rule.Level.DimensionKey}Ids"),
            _ => "AND 1 = 0"
        };
    }

    #endregion

    #region Policy API

    /// <summary>Build SQL WHERE clause from a policy (multiple rules combined).</summary>
    public static string Build(DataScopePolicy policy, DynamicParameters parameters)
        => Build(policy, parameters, new SqlScopeColumnMapping());

    /// <summary>Build SQL WHERE clause from a policy with custom column mapping.</summary>
    public static string Build(DataScopePolicy policy, DynamicParameters parameters, SqlScopeColumnMapping columns)
    {
        var simplified = policy.Simplify();

        if (simplified.IsSingleRule)
            return Build(simplified.SingleRule!, parameters, columns);

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

    /// <summary>Build standalone WHERE clause from policy (without leading AND).</summary>
    public static string BuildStandalone(DataScopePolicy policy, DynamicParameters parameters)
        => BuildStandalone(policy, parameters, new SqlScopeColumnMapping());

    /// <summary>Build standalone WHERE clause from policy with custom columns.</summary>
    public static string BuildStandalone(DataScopePolicy policy, DynamicParameters parameters, SqlScopeColumnMapping columns)
    {
        var simplified = policy.Simplify();

        if (simplified.IsSingleRule)
            return BuildStandalone(simplified.SingleRule!, parameters, columns);

        var clauses = new List<string>();
        var counter = 0;

        foreach (var rule in simplified.Rules)
        {
            var clause = BuildRuleClause(rule, parameters, columns, counter++);
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

    #region Legacy Single Rule API

    /// <summary>Build for employee-based queries with join to EmployeeAssignments.</summary>
    public static string BuildWithAssignments(
        DataScopeRule rule,
        DynamicParameters parameters,
        string employeeAlias = "e",
        string assignmentAlias = "ea")
    {
        var columns = new SqlScopeColumnMapping
        {
            OwnerColumn = $"{employeeAlias}.Id",
            CompanyColumn = $"{assignmentAlias}.CompanyId",
            DepartmentColumn = $"{assignmentAlias}.DepartmentId",
            PositionColumn = $"{assignmentAlias}.PositionId",
            CountryColumn = $"{employeeAlias}.CountryId",
            RegionColumn = $"{employeeAlias}.RegionId"
        };

        return rule.Level.Category switch
        {
            ScopeCategory.Global => string.Empty,
            ScopeCategory.None => "AND 1 = 0",
            ScopeCategory.Set when rule.Level == DataScopeLevel.Self && rule.SelfEmployeeId.HasValue =>
                BuildSelfFilter(rule.SelfEmployeeId.Value, parameters, columns.OwnerColumn, "@ScopeEmployeeId"),
            ScopeCategory.Set when rule.EmployeeIds.Count > 0 =>
                BuildEmployeeSetFilter(rule.EmployeeIds, parameters, columns.OwnerColumn),
            ScopeCategory.Dimension when rule.Level.DimensionKey is not null =>
                BuildDimensionFilter(rule.DimensionIds, parameters,
                    columns.GetColumn(rule.Level.DimensionKey),
                    $"@Scope{rule.Level.DimensionKey}Ids"),
            _ => "AND 1 = 0"
        };
    }

    /// <summary>Build standalone WHERE clause (without leading AND).</summary>
    public static string BuildStandalone(DataScopeRule rule, DynamicParameters parameters)
        => BuildStandalone(rule, parameters, new SqlScopeColumnMapping());

    /// <summary>Build standalone WHERE clause with custom column mapping.</summary>
    public static string BuildStandalone(DataScopeRule rule, DynamicParameters parameters, SqlScopeColumnMapping columns)
    {
        return rule.Level.Category switch
        {
            ScopeCategory.Global => "1 = 1",
            ScopeCategory.None => "1 = 0",
            ScopeCategory.Set when rule.Level == DataScopeLevel.Self && rule.SelfEmployeeId.HasValue =>
                BuildStandaloneSelf(rule.SelfEmployeeId.Value, parameters, columns.OwnerColumn),
            ScopeCategory.Set when rule.EmployeeIds.Count > 0 =>
                BuildStandaloneEmployeeSet(rule.EmployeeIds, parameters, columns.OwnerColumn),
            ScopeCategory.Dimension when rule.Level.DimensionKey is not null =>
                BuildStandaloneDimension(rule.DimensionIds, parameters,
                    columns.GetColumn(rule.Level.DimensionKey),
                    $"@Scope{rule.Level.DimensionKey}Ids"),
            _ => "1 = 0"
        };
    }

    #endregion

    #region Private Helpers

    private static string BuildRuleClause(
        DataScopeRule rule, DynamicParameters parameters, SqlScopeColumnMapping columns, int counter)
    {
        var suffix = counter > 0 ? $"_{counter}" : "";

        return rule.Level.Category switch
        {
            ScopeCategory.Global => "1 = 1",
            ScopeCategory.None => "1 = 0",
            ScopeCategory.Set when rule.Level == DataScopeLevel.Self && rule.SelfEmployeeId.HasValue =>
                BuildSelfClause(rule.SelfEmployeeId.Value, parameters, columns.OwnerColumn, $"@ScopeEmployeeId{suffix}"),
            ScopeCategory.Set when rule.EmployeeIds.Count > 0 =>
                BuildEmployeeSetClause(rule.EmployeeIds, parameters, columns.OwnerColumn, $"@ScopeEmployeeIds{suffix}"),
            ScopeCategory.Dimension when rule.Level.DimensionKey is not null =>
                BuildDimensionClause(rule.DimensionIds, parameters,
                    columns.GetColumn(rule.Level.DimensionKey),
                    $"@Scope{rule.Level.DimensionKey}Ids{suffix}"),
            _ => "1 = 0"
        };
    }

    private static string BuildDimensionFilter(
        IReadOnlyCollection<Guid> ids, DynamicParameters parameters, string column, string paramName)
    {
        parameters.Add(paramName, ids);
        return $"AND {column} IN {paramName}";
    }

    private static string BuildDimensionClause(
        IReadOnlyCollection<Guid> ids, DynamicParameters parameters, string column, string paramName)
    {
        parameters.Add(paramName, ids);
        return $"{column} IN {paramName}";
    }

    private static string BuildSelfFilter(
        Guid employeeId, DynamicParameters parameters, string column, string paramName = "@ScopeEmployeeId")
    {
        parameters.Add(paramName, employeeId);
        return $"AND {column} = {paramName}";
    }

    private static string BuildSelfClause(
        Guid employeeId, DynamicParameters parameters, string column, string paramName)
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
        IReadOnlyCollection<Guid> ids, DynamicParameters parameters, string column, string paramName)
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
/// Supports dynamic dimension key → column name lookup.
/// </summary>
public sealed class SqlScopeColumnMapping
{
    private readonly Dictionary<string, string> _dimensionColumns = new(StringComparer.OrdinalIgnoreCase);

    public string CompanyColumn
    {
        get => GetColumn("Company");
        init => _dimensionColumns["Company"] = value;
    }

    public string DepartmentColumn
    {
        get => GetColumn("Department");
        init => _dimensionColumns["Department"] = value;
    }

    public string PositionColumn
    {
        get => GetColumn("Position");
        init => _dimensionColumns["Position"] = value;
    }

    public string CountryColumn
    {
        get => GetColumn("Country");
        init => _dimensionColumns["Country"] = value;
    }

    public string RegionColumn
    {
        get => GetColumn("Region");
        init => _dimensionColumns["Region"] = value;
    }

    public string OwnerColumn { get; init; } = "OwnerId";
    public string EmployeeColumn { get; init; } = "EmployeeId";

    /// <summary>
    /// Get column name for a dimension key.
    /// Falls back to "{DimensionKey}Id" if not explicitly mapped.
    /// </summary>
    public string GetColumn(string dimensionKey)
        => _dimensionColumns.TryGetValue(dimensionKey, out var col) ? col : $"{dimensionKey}Id";

    /// <summary>Set column name for a dimension key.</summary>
    public void SetColumn(string dimensionKey, string columnName)
        => _dimensionColumns[dimensionKey] = columnName;
}
