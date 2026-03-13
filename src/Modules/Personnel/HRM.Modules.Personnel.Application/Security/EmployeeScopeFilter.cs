using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Personnel.Application.Abstractions.Data;
using HRM.Modules.Personnel.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Personnel.Application.Security;

/// <summary>
/// Shared helper for employee-level scope checks based on a DataScopeRule.
///
/// For dimension-based scopes (Company, Department, Position), access is determined
/// by the employee's ACTIVE ASSIGNMENTS — not just the primary assignment.
/// An employee is accessible if they have ANY active assignment matching the scope dimensions.
///
/// For list queries, use ApplyScope to generate a composable EF WHERE clause.
/// For single-entity checks (commands), use IsAccessibleAsync.
/// </summary>
public static class EmployeeScopeFilter
{
    /// <summary>
    /// Check whether a specific employee is accessible under the given data scope rule.
    /// For dimension-based scopes, checks the Assignments table (not just PrimaryXId).
    /// </summary>
    public static async Task<bool> IsAccessibleAsync(
        DataScopeRule rule,
        Guid employeeId,
        IPersonnelQueryContext context,
        CancellationToken cancellationToken = default)
    {
        return rule.Level switch
        {
            DataScopeLevel.Global => true,
            DataScopeLevel.None => false,
            DataScopeLevel.Self => employeeId == rule.SelfEmployeeId,
            DataScopeLevel.DirectReports or DataScopeLevel.EmployeeSet =>
                rule.EmployeeIds.Contains(employeeId),
            DataScopeLevel.Company or DataScopeLevel.Department or DataScopeLevel.Position =>
                await HasMatchingAssignmentAsync(rule, employeeId, context, cancellationToken),
            _ => false
        };
    }

    /// <summary>
    /// Apply a DataScopeRule as an EF WHERE clause to an employees query.
    /// For dimension-based scopes, filters via EmployeeAssignments JOIN.
    /// </summary>
    public static IQueryable<Employee> ApplyScope(
        IQueryable<Employee> query,
        DataScopeRule rule,
        IPersonnelQueryContext context)
    {
        return rule.Level switch
        {
            DataScopeLevel.Global => query,
            DataScopeLevel.None => query.Where(_ => false),
            DataScopeLevel.Self => query.Where(e => e.OwnerId == rule.SelfEmployeeId!.Value),
            DataScopeLevel.DirectReports or DataScopeLevel.EmployeeSet =>
                query.Where(e => rule.EmployeeIds.Contains(e.OwnerId)),
            DataScopeLevel.Company or DataScopeLevel.Department or DataScopeLevel.Position =>
                ApplyDimensionScope(query, rule, context),
            _ => query.Where(_ => false)
        };
    }

    private static async Task<bool> HasMatchingAssignmentAsync(
        DataScopeRule rule,
        Guid employeeId,
        IPersonnelQueryContext context,
        CancellationToken cancellationToken)
    {
        var ids = rule.DimensionIds.ToList();

        var assignmentQuery = context.EmployeeAssignments
            .AsNoTracking()
            .Where(a => a.EmployeeId == employeeId
                && a.Status == AssignmentStatus.Active
                && !a.EndDate.HasValue);

        return rule.Level switch
        {
            DataScopeLevel.Company =>
                await assignmentQuery.AnyAsync(a => ids.Contains(a.CompanyId), cancellationToken),
            DataScopeLevel.Department =>
                await assignmentQuery.AnyAsync(a => ids.Contains(a.DepartmentId), cancellationToken),
            DataScopeLevel.Position =>
                await assignmentQuery.AnyAsync(a => ids.Contains(a.PositionId), cancellationToken),
            _ => false
        };
    }

    private static IQueryable<Employee> ApplyDimensionScope(
        IQueryable<Employee> query,
        DataScopeRule rule,
        IPersonnelQueryContext context)
    {
        var ids = rule.DimensionIds.ToList();

        var employeeIdsWithAccess = rule.Level switch
        {
            DataScopeLevel.Company => context.EmployeeAssignments
                .Where(a => a.Status == AssignmentStatus.Active && !a.EndDate.HasValue && ids.Contains(a.CompanyId))
                .Select(a => a.EmployeeId),
            DataScopeLevel.Department => context.EmployeeAssignments
                .Where(a => a.Status == AssignmentStatus.Active && !a.EndDate.HasValue && ids.Contains(a.DepartmentId))
                .Select(a => a.EmployeeId),
            DataScopeLevel.Position => context.EmployeeAssignments
                .Where(a => a.Status == AssignmentStatus.Active && !a.EndDate.HasValue && ids.Contains(a.PositionId))
                .Select(a => a.EmployeeId),
            _ => context.EmployeeAssignments.Where(_ => false).Select(a => a.EmployeeId)
        };

        return query.Where(e => employeeIdsWithAccess.Contains(e.Id));
    }
}
