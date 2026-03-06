namespace HRM.Modules.Personnel.Infrastructure.Persistence;

/// <summary>
/// Closure table for employee hierarchy.
///
/// Materializes all ancestor-descendant relationships at every depth level,
/// enabling O(1) hierarchy queries (subtree, management chain, IsSubordinate).
///
/// Schema:
///   AncestorId   = the manager (or any ancestor)
///   DescendantId = the employee (or any descendant)
///   Depth        = 0 for self-reference row (existence marker), 1 for direct report, etc.
///
/// Self-reference row (Depth=0): every employee has a row where AncestorId == DescendantId.
/// This allows "subtree of X" queries to naturally include X itself.
///
/// Maintained by:
///   - EmployeeCreatedHierarchyHandler  → inserts self-ref + parent chain on creation
///   - ManagerChangedDomainEventHandler → Celko prune+graft on manager change
///   - RebuildHierarchyAsync            → full rebuild for bulk import / data recovery
/// </summary>
public sealed class EmployeeHierarchyClosure
{
    public Guid TenantId { get; set; }
    public Guid AncestorId { get; set; }
    public Guid DescendantId { get; set; }
    public int Depth { get; set; }
}
