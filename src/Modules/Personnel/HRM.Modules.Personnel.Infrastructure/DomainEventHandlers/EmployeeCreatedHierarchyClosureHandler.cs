using HRM.Modules.Personnel.Domain.Events;
using HRM.Modules.Personnel.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Personnel.Infrastructure.DomainEventHandlers;

/// <summary>
/// Initializes the closure table for a newly created employee.
///
/// This handler runs BEFORE SaveChanges (ModuleDbContext dispatches domain events prior to
/// calling base.SaveChangesAsync). Therefore it uses EF Core change tracking (.Add) rather
/// than raw SQL — all entries are persisted atomically with the employee entity.
///
/// Entries inserted:
/// 1. Self-reference row: (employeeId → employeeId, depth=0)
///    Acts as existence marker; presence confirms the closure table is populated for this employee.
/// 2. Ancestor chain rows (if manager is set): for each ancestor of the manager in the
///    closure table, insert (ancestor → employeeId, depth = ancestor.depth + 1).
///    The manager's ancestors are already in the DB from their own creation/assignment.
/// </summary>
internal sealed class EmployeeCreatedHierarchyClosureHandler
    : INotificationHandler<EmployeeCreatedDomainEvent>
{
    private readonly PersonnelDbContext _dbContext;

    public EmployeeCreatedHierarchyClosureHandler(PersonnelDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(EmployeeCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        var employeeId = notification.EmployeeId;
        var tenantId = notification.TenantId;

        // Self-reference row — existence marker and self-scope foundation.
        _dbContext.EmployeeHierarchyClosures.Add(new EmployeeHierarchyClosure
        {
            AncestorId = employeeId,
            DescendantId = employeeId,
            Depth = 0,
            TenantId = tenantId
        });

        if (!notification.ManagerId.HasValue)
            return;

        // Wire the new employee into the existing hierarchy:
        // For each ancestor of the manager (including the manager themselves),
        // create a link: ancestor → new employee.
        var managerId = notification.ManagerId.Value;

        var managerAncestors = await _dbContext.EmployeeHierarchyClosures
            .AsNoTracking()
            .Where(c => c.DescendantId == managerId && c.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        foreach (var ancestor in managerAncestors)
        {
            _dbContext.EmployeeHierarchyClosures.Add(new EmployeeHierarchyClosure
            {
                AncestorId = ancestor.AncestorId,
                DescendantId = employeeId,
                Depth = ancestor.Depth + 1,
                TenantId = tenantId
            });
        }
    }
}
