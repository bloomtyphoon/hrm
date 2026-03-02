namespace HRM.Modules.Personnel.Infrastructure.Persistence;

/// <summary>
/// Materialized closure table for the employee management hierarchy.
///
/// Each row represents a reachable (AncestorId → DescendantId) path at a given depth.
/// A self-reference row (AncestorId == DescendantId, Depth == 0) is maintained for
/// every employee, which also serves as an existence marker for cache-vs-table checks.
///
/// Maintained by:
/// - EmployeeCreatedHierarchyClosureHandler — inserts self-reference on employee creation
/// - ManagerChangedDomainEventHandler — grafts subtree when manager assignment changes
///
/// Query pattern (all subordinates of a manager):
///   SELECT DescendantId FROM EmployeeHierarchyClosures
///   WHERE AncestorId = @managerId AND TenantId = @tenantId
/// </summary>
internal sealed class EmployeeHierarchyClosure
{
    /// <summary>The ancestor (manager at any level, or the employee themselves for self-reference).</summary>
    public Guid AncestorId { get; set; }

    /// <summary>The descendant (subordinate at any depth, or the employee themselves for self-reference).</summary>
    public Guid DescendantId { get; set; }

    /// <summary>Number of edges between ancestor and descendant. 0 = self-reference.</summary>
    public int Depth { get; set; }

    /// <summary>Tenant ID — used to scope closure table queries to the correct tenant.</summary>
    public Guid TenantId { get; set; }
}
