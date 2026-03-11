namespace HRM.BuildingBlocks.Application.Abstractions.Authorization;

/// <summary>
/// Contract for resolving hierarchical scope (manager → subordinates).
///
/// This interface lives in BuildingBlocks as a CONTRACT.
/// Implementation lives in the Personnel module that owns the employee hierarchy.
///
/// Design (separation of concerns):
/// - Identity module: Determines scope LEVEL (Self, EmployeeSet, etc.)
/// - Personnel module: Resolves scope MEMBERS (which employee IDs)
///
/// Flow:
/// 1. Identity determines user has EmployeeSet scope level
/// 2. Identity calls IHierarchyScopeResolver to get subordinate IDs
/// 3. Identity creates DataScopeRule.EmployeeSet(subordinateIds)
/// 4. Query handlers apply the rule via EfScopeExpressionBuilder
///
/// Self-inclusion semantics (confirmed):
/// - ResolveAllSubordinatesAsync  includes manager themselves (Depth=0 row)
/// - ResolveDirectSubordinatesAsync includes manager themselves
/// Rationale: managers always need to see their own data alongside their team's data.
/// </summary>
public interface IHierarchyScopeResolver
{
    /// <summary>
    /// Get all subordinate employee IDs for a manager (recursive).
    /// Includes the manager themselves.
    /// Uses closure table — O(1) single-query lookup.
    /// </summary>
    Task<IReadOnlySet<Guid>> ResolveAllSubordinatesAsync(
        Guid managerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get direct subordinates only (Depth=1), plus the manager themselves.
    /// </summary>
    Task<IReadOnlySet<Guid>> ResolveDirectSubordinatesAsync(
        Guid managerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if one employee is a subordinate of another.
    /// Uses closure table EXISTS — O(1), no subtree load.
    /// </summary>
    Task<bool> IsSubordinateOfAsync(
        Guid employeeId,
        Guid managerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the management chain for an employee (bottom-up).
    /// Returns [employee, directManager, manager's manager, ...] ordered from self to root.
    /// Uses closure table — single query, no N+1.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetManagementChainAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default);
}
