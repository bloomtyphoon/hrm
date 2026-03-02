using HRM.BuildingBlocks.Application.Abstractions.Caching;
using HRM.Modules.Personnel.Domain.Events;
using HRM.Modules.Personnel.Infrastructure.Services;
using MediatR;

namespace HRM.Modules.Personnel.Infrastructure.DomainEventHandlers;

/// <summary>
/// Invalidates hierarchy scope caches when an employee's manager changes.
///
/// When a manager assignment changes (AssignManager / RemoveManager), the subordinate
/// trees of the previous manager, the new manager, and all their ancestors are affected.
/// Rather than tracking the full ancestor chain, we invalidate all hierarchy cache entries
/// for the tenant — a broad but safe strategy until a closure table is introduced.
///
/// Cache key pattern invalidated: personnel:{tenantId}:hierarchy:*
/// </summary>
internal sealed class ManagerChangedDomainEventHandler
    : INotificationHandler<ManagerChangedDomainEvent>
{
    private readonly ICache _cache;

    public ManagerChangedDomainEventHandler(ICache cache)
    {
        _cache = cache;
    }

    public async Task Handle(ManagerChangedDomainEvent notification, CancellationToken cancellationToken)
    {
        var prefix = HierarchyScopeResolver.GetTenantHierarchyPrefix(notification.TenantId);
        await _cache.RemoveByPrefixAsync(prefix);
    }
}
