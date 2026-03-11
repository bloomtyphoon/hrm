using HRM.Modules.Personnel.Domain.Events;
using HRM.Modules.Personnel.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Personnel.Infrastructure.DomainEventHandlers;

/// <summary>
/// Initializes the closure table when a new employee is created.
///
/// Inserts:
///   1. Self-reference row  (AncestorId = DescendantId = EmployeeId, Depth = 0)
///   2. Ancestor chain rows (one row per ancestor inherited from manager's subtree)
///
/// If the employee has no manager, only the self-reference row is inserted.
/// </summary>
internal sealed class EmployeeCreatedHierarchyHandler
    : INotificationHandler<EmployeeCreatedDomainEvent>
{
    private readonly PersonnelDbContext _dbContext;

    public EmployeeCreatedHierarchyHandler(PersonnelDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(
        EmployeeCreatedDomainEvent notification,
        CancellationToken cancellationToken)
    {
        // Always insert self-reference row (Depth = 0)
        _dbContext.EmployeeHierarchyClosures.Add(new EmployeeHierarchyClosure
        {
            TenantId     = notification.TenantId,
            AncestorId   = notification.EmployeeId,
            DescendantId = notification.EmployeeId,
            Depth        = 0
        });

        if (notification.ManagerId.HasValue)
        {
            // Connect all ancestors of the manager to this new employee.
            // One row per ancestor in the manager's lineage.
            var managerAncestors = await _dbContext.EmployeeHierarchyClosures
                .Where(c =>
                    c.TenantId     == notification.TenantId &&
                    c.DescendantId == notification.ManagerId.Value)
                .ToListAsync(cancellationToken);

            foreach (var ancestorRow in managerAncestors)
            {
                _dbContext.EmployeeHierarchyClosures.Add(new EmployeeHierarchyClosure
                {
                    TenantId     = notification.TenantId,
                    AncestorId   = ancestorRow.AncestorId,
                    DescendantId = notification.EmployeeId,
                    Depth        = ancestorRow.Depth + 1
                });
            }
        }
    }
}
