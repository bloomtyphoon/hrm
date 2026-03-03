using HRM.Modules.Personnel.Domain.Events;
using HRM.Modules.Personnel.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Personnel.Infrastructure.DomainEventHandlers;

/// <summary>
/// Maintains the closure table when an employee's manager changes.
///
/// Implements the Celko 2-step prune+graft algorithm:
///
///   PRUNE  – Delete all rows that connect the employee's OLD ancestors
///            to the employee's subtree (descendants including self).
///
///   GRAFT  – Insert new rows connecting the NEW manager's ancestors
///            to the employee's subtree.
///
/// If NewManagerId is null (manager removed), only PRUNE is executed —
/// the employee becomes a root node with only self-reference rows.
///
/// Reference: Joe Celko "SQL for Smarties" ch. 36 (Closure Tables)
/// </summary>
internal sealed class ManagerChangedDomainEventHandler
    : INotificationHandler<ManagerChangedDomainEvent>
{
    private readonly PersonnelDbContext _dbContext;

    public ManagerChangedDomainEventHandler(PersonnelDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(
        ManagerChangedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        var tenantId    = notification.TenantId;
        var employeeId  = notification.EmployeeId;

        // ── STEP 1: PRUNE ──────────────────────────────────────────────────
        // Remove rows that link any OLD ancestor to any descendant of employeeId.
        // Keep self-reference rows (Depth = 0) — they are existence markers.

        // Collect IDs of all descendants (including self)
        var descendantIds = await _dbContext.EmployeeHierarchyClosures
            .Where(c =>
                c.TenantId   == tenantId &&
                c.AncestorId == employeeId)
            .Select(c => c.DescendantId)
            .ToListAsync(cancellationToken);

        // Collect IDs of all OLD ancestors (excluding self, Depth > 0)
        var oldAncestorIds = await _dbContext.EmployeeHierarchyClosures
            .Where(c =>
                c.TenantId     == tenantId &&
                c.DescendantId == employeeId &&
                c.Depth        > 0)
            .Select(c => c.AncestorId)
            .ToListAsync(cancellationToken);

        if (oldAncestorIds.Count > 0)
        {
            // Delete all rows: old-ancestor → any-descendant-of-employee
            var rowsToRemove = await _dbContext.EmployeeHierarchyClosures
                .Where(c =>
                    c.TenantId     == tenantId &&
                    oldAncestorIds.Contains(c.AncestorId) &&
                    descendantIds.Contains(c.DescendantId))
                .ToListAsync(cancellationToken);

            _dbContext.EmployeeHierarchyClosures.RemoveRange(rowsToRemove);
        }

        // ── STEP 2: GRAFT ──────────────────────────────────────────────────
        // If a new manager is specified, connect the new manager's ancestors
        // to the employee's full subtree (descendants including self).

        if (notification.NewManagerId.HasValue)
        {
            // Ancestors of the new manager (including the manager themselves via Depth=0)
            var newManagerAncestors = await _dbContext.EmployeeHierarchyClosures
                .Where(c =>
                    c.TenantId     == tenantId &&
                    c.DescendantId == notification.NewManagerId.Value)
                .ToListAsync(cancellationToken);

            // Subtree of the moved employee (descendants including self)
            var employeeSubtree = await _dbContext.EmployeeHierarchyClosures
                .Where(c =>
                    c.TenantId   == tenantId &&
                    c.AncestorId == employeeId)
                .ToListAsync(cancellationToken);

            // Cross-join: one new row per (ancestor, descendant) combination
            // New depth = ancestor.Depth + descendant.Depth + 1
            //   (+1 because ancestor connects to employeeId, then subtree continues)
            var newRows = newManagerAncestors
                .SelectMany(
                    _ => employeeSubtree,
                    (anc, desc) => new EmployeeHierarchyClosure
                    {
                        TenantId     = tenantId,
                        AncestorId   = anc.AncestorId,
                        DescendantId = desc.DescendantId,
                        Depth        = anc.Depth + desc.Depth + 1
                    })
                .ToList();

            _dbContext.EmployeeHierarchyClosures.AddRange(newRows);
        }
    }
}
