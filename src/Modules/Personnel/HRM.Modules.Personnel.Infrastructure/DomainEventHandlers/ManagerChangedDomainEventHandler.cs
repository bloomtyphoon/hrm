using HRM.BuildingBlocks.Application.Abstractions.Caching;
using HRM.Modules.Personnel.Domain.Events;
using HRM.Modules.Personnel.Infrastructure.Persistence;
using HRM.Modules.Personnel.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Personnel.Infrastructure.DomainEventHandlers;

/// <summary>
/// Maintains the hierarchy closure table and invalidates hierarchy caches
/// when an employee's manager changes.
///
/// This handler runs BEFORE SaveChanges (ModuleDbContext pattern), so it uses
/// raw SQL via ExecuteSqlRawAsync — the closure table entries are already in the
/// DB (committed in previous transactions), and we need immediate SQL to graft
/// the subtree correctly.
///
/// Closure table graft algorithm (Celko):
///
/// When employee E moves from OldManager to NewManager:
///
/// Step 1 — Prune old cross-tree links:
///   Delete links where:
///   - descendant is E or any of E's subordinates
///   - ancestor is any ancestor of E (i.e., above E, not within E's subtree)
///
/// Step 2 — Graft new cross-tree links (only if NewManager is not null):
///   Insert links: for each ancestor of NewManager × each descendant of E.
///   Depth = ancestor.Depth + 1 + descendant.Depth
///
/// Cache invalidation:
///   Removes all cached hierarchy lookups for the tenant (broad but safe).
/// </summary>
internal sealed class ManagerChangedDomainEventHandler
    : INotificationHandler<ManagerChangedDomainEvent>
{
    private readonly PersonnelDbContext _dbContext;
    private readonly ICache _cache;

    public ManagerChangedDomainEventHandler(PersonnelDbContext dbContext, ICache cache)
    {
        _dbContext = dbContext;
        _cache = cache;
    }

    public async Task Handle(ManagerChangedDomainEvent notification, CancellationToken cancellationToken)
    {
        var employeeId = notification.EmployeeId;
        var tenantId = notification.TenantId;
        var newManagerId = notification.NewManagerId;

        // Step 1: Prune all cross-tree entries:
        //   Delete rows where the descendant is in E's subtree
        //   AND the ancestor is NOT in E's subtree (i.e., it is from the old parent chain).
        //
        // SQL: DELETE rows where descendant ∈ descendants(E) AND ancestor ∉ descendants(E)
        var pruneSQL = @"
            DELETE link
            FROM Personnel.EmployeeHierarchyClosures link
            INNER JOIN Personnel.EmployeeHierarchyClosures sub
                ON link.DescendantId = sub.DescendantId
                AND sub.AncestorId = {0}
                AND sub.TenantId = {1}
            WHERE link.TenantId = {1}
              AND link.AncestorId NOT IN (
                  SELECT DescendantId
                  FROM Personnel.EmployeeHierarchyClosures
                  WHERE AncestorId = {0} AND TenantId = {1}
              );";

        await _dbContext.Database.ExecuteSqlRawAsync(
            pruneSQL, new object[] { employeeId, tenantId }, cancellationToken);

        // Step 2: Graft into new position (skip if employee becomes a root node).
        if (newManagerId.HasValue)
        {
            // Insert: for every ancestor of the new manager × every descendant of E.
            // Depth = (new manager ancestor depth) + 1 + (descendant-of-E depth from E).
            var graftSQL = @"
                INSERT INTO Personnel.EmployeeHierarchyClosures (AncestorId, DescendantId, Depth, TenantId)
                SELECT sup.AncestorId, sub.DescendantId, sup.Depth + sub.Depth + 1, {1}
                FROM Personnel.EmployeeHierarchyClosures sup
                CROSS JOIN Personnel.EmployeeHierarchyClosures sub
                WHERE sup.DescendantId = {2}
                  AND sup.TenantId = {1}
                  AND sub.AncestorId = {0}
                  AND sub.TenantId = {1};";

            await _dbContext.Database.ExecuteSqlRawAsync(
                graftSQL, new object[] { employeeId, tenantId, newManagerId.Value }, cancellationToken);
        }

        // Invalidate all hierarchy caches for this tenant.
        var prefix = HierarchyScopeResolver.GetTenantHierarchyPrefix(tenantId);
        await _cache.RemoveByPrefixAsync(prefix);
    }
}
