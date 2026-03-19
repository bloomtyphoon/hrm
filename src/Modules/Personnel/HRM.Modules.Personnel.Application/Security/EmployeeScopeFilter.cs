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
/// Uses category-based dispatch for dynamic scope level support.
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
        return rule.Level.Category switch
        {
            ScopeCategory.Global => true,
            ScopeCategory.None => false,
            ScopeCategory.Set when rule.Level == DataScopeLevel.Self =>
                employeeId == rule.SelfEmployeeId,
            ScopeCategory.Set =>
                rule.EmployeeIds.Contains(employeeId),
            ScopeCategory.Dimension =>
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
        return rule.Level.Category switch
        {
            ScopeCategory.Global => query,
            ScopeCategory.None => query.Where(_ => false),
            ScopeCategory.Set when rule.Level == DataScopeLevel.Self =>
                query.Where(e => e.OwnerId == rule.SelfEmployeeId!.Value),
            ScopeCategory.Set =>
                query.Where(e => rule.EmployeeIds.Contains(e.OwnerId)),
            ScopeCategory.Dimension =>
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

        // Use DimensionKey to determine which assignment column to check
        return rule.Level.DimensionKey switch
        {
            DimensionKeys.Company =>
                await assignmentQuery.AnyAsync(a => ids.Contains(a.CompanyId), cancellationToken),
            DimensionKeys.Department =>
                await assignmentQuery.AnyAsync(a => ids.Contains(a.DepartmentId), cancellationToken),
            DimensionKeys.Position =>
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

        // Use DimensionKey to determine which assignment column to filter
        var employeeIdsWithAccess = rule.Level.DimensionKey switch
        {
            DimensionKeys.Company => context.EmployeeAssignments
                .Where(a => a.Status == AssignmentStatus.Active && !a.EndDate.HasValue && ids.Contains(a.CompanyId))
                .Select(a => a.EmployeeId),
            DimensionKeys.Department => context.EmployeeAssignments
                .Where(a => a.Status == AssignmentStatus.Active && !a.EndDate.HasValue && ids.Contains(a.DepartmentId))
                .Select(a => a.EmployeeId),
            DimensionKeys.Position => context.EmployeeAssignments
                .Where(a => a.Status == AssignmentStatus.Active && !a.EndDate.HasValue && ids.Contains(a.PositionId))
                .Select(a => a.EmployeeId),
            _ => context.EmployeeAssignments.Where(_ => false).Select(a => a.EmployeeId)
        };

        return query.Where(e => employeeIdsWithAccess.Contains(e.Id));
    }
}
