using Dapper;
using HRM.BuildingBlocks.Domain.Abstractions.Security;

namespace HRM.BuildingBlocks.Infrastructure.Security;

/// <summary>
/// Translates DataScopeRule to SQL WHERE clause.
///
/// IMPORTANT: This class contains NO business logic.
/// It only translates the level-based rule to SQL format.
/// All business logic is in IDataScopeService / IDataScopeRuleProvider.
///
/// Usage:
/// <code>
/// var rule = await dataScopeService.GetScopeRuleAsync(userId, permission);
/// var parameters = new DynamicParameters();
/// var where = SqlScopeWhereBuilder.Build(rule, parameters);
/// var sql = $"SELECT * FROM Employees e WHERE 1=1 {where}";
/// var result = await connection.QueryAsync(sql, parameters);
/// </code>
/// </summary>
public static class SqlScopeWhereBuilder
{
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

            DataScopeLevel.Company => BuildDimensionFilter(
                rule.DimensionIds, parameters, columns.CompanyColumn, "@ScopeCompanyIds"),

            DataScopeLevel.Department => BuildDimensionFilter(
                rule.DimensionIds, parameters, columns.DepartmentColumn, "@ScopeDepartmentIds"),

            DataScopeLevel.Position => BuildDimensionFilter(
                rule.DimensionIds, parameters, columns.PositionColumn, "@ScopePositionIds"),

            DataScopeLevel.Self when rule.SelfEmployeeId.HasValue => BuildSelfFilter(
                rule.SelfEmployeeId.Value, parameters, columns.OwnerColumn),

            _ => "AND 1 = 0"
        };
    }

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

            DataScopeLevel.Company => BuildDimensionFilter(
                rule.DimensionIds, parameters, $"{assignmentAlias}.CompanyId", "@ScopeCompanyIds"),

            DataScopeLevel.Department => BuildDimensionFilter(
                rule.DimensionIds, parameters, $"{assignmentAlias}.DepartmentId", "@ScopeDepartmentIds"),

            DataScopeLevel.Position => BuildDimensionFilter(
                rule.DimensionIds, parameters, $"{assignmentAlias}.PositionId", "@ScopePositionIds"),

            DataScopeLevel.Self when rule.SelfEmployeeId.HasValue => BuildSelfFilter(
                rule.SelfEmployeeId.Value, parameters, $"{employeeAlias}.Id", "@ScopeEmployeeId"),

            _ => "AND 1 = 0"
        };
    }

    /// <summary>
    /// Build standalone WHERE clause (without leading AND).
    /// </summary>
    public static string BuildStandalone(DataScopeRule rule, DynamicParameters parameters)
    {
        return rule.Level switch
        {
            DataScopeLevel.Global => "1 = 1",

            DataScopeLevel.Company => BuildStandaloneDimension(
                rule.DimensionIds, parameters, "CompanyId", "@ScopeCompanyIds"),

            DataScopeLevel.Department => BuildStandaloneDimension(
                rule.DimensionIds, parameters, "DepartmentId", "@ScopeDepartmentIds"),

            DataScopeLevel.Position => BuildStandaloneDimension(
                rule.DimensionIds, parameters, "PositionId", "@ScopePositionIds"),

            DataScopeLevel.Self when rule.SelfEmployeeId.HasValue =>
                BuildStandaloneSelf(rule.SelfEmployeeId.Value, parameters, "OwnerId"),

            _ => "1 = 0"
        };
    }

    private static string BuildDimensionFilter(
        IReadOnlyCollection<Guid> ids, DynamicParameters parameters,
        string column, string paramName)
    {
        parameters.Add(paramName, ids);
        return $"AND {column} IN {paramName}";
    }

    private static string BuildSelfFilter(
        Guid employeeId, DynamicParameters parameters,
        string column, string paramName = "@ScopeEmployeeId")
    {
        parameters.Add(paramName, employeeId);
        return $"AND {column} = {paramName}";
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
}

/// <summary>
/// Column name mapping for SQL scope queries.
/// Override defaults when table uses different column names.
/// </summary>
public sealed class SqlScopeColumnMapping
{
    public string CompanyColumn { get; init; } = "CompanyId";
    public string DepartmentColumn { get; init; } = "DepartmentId";
    public string PositionColumn { get; init; } = "PositionId";
    public string OwnerColumn { get; init; } = "OwnerId";
    public string EmployeeColumn { get; init; } = "EmployeeId";
}
