using HRM.BuildingBlocks.Domain.Abstractions.Security;

namespace HRM.BuildingBlocks.Application.Abstractions.Authorization;

/// <summary>
/// Contract for resolving hierarchical scope (manager → subordinates).
///
/// This interface lives in BuildingBlocks as a CONTRACT.
/// Implementation lives in the Organization module that owns the employee hierarchy.
///
/// Design (separation of concerns):
/// - Identity module: Determines scope LEVEL (Self, EmployeeSet, etc.)
/// - Organization module: Resolves scope MEMBERS (which employee IDs)
///
/// Flow:
/// 1. Identity determines user has EmployeeSet scope level
/// 2. Identity calls IHierarchyScopeResolver to get subordinate IDs
/// 3. Identity creates DataScopeRule.EmployeeSet(subordinateIds)
/// 4. Query handlers apply the rule via EfScopeExpressionBuilder
///
/// Hierarchy Types:
/// - Direct: Only immediate subordinates
/// - Recursive: All subordinates (subordinates of subordinates)
/// - Custom: Based on explicit manager assignments
/// </summary>
public interface IHierarchyScopeResolver
{
    /// <summary>
    /// Get all subordinate employee IDs for a manager.
    /// Includes the manager themselves (they can see their own data).
    /// </summary>
    /// <param name="managerId">Manager's employee ID</param>
    /// <param name="includeIndirect">True to include indirect reports (recursive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Set of employee IDs including manager and subordinates</returns>
    Task<IReadOnlyCollection<Guid>> GetSubordinateIdsAsync(
        Guid managerId,
        bool includeIndirect = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get direct subordinates only (no recursion).
    /// </summary>
    Task<IReadOnlyCollection<Guid>> GetDirectSubordinateIdsAsync(
        Guid managerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if one employee is a subordinate of another.
    /// </summary>
    Task<bool> IsSubordinateOfAsync(
        Guid employeeId,
        Guid managerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the management chain for an employee (bottom-up).
    /// Returns [employee, directManager, manager's manager, ...] up to root.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetManagementChainAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default);
}
