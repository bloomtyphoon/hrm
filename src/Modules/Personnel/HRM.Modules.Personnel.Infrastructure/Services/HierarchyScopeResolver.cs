using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Caching;
using HRM.BuildingBlocks.Application.Abstractions.Multitenancy;
using HRM.Modules.Personnel.Application.Abstractions;

namespace HRM.Modules.Personnel.Infrastructure.Services;

/// <summary>
/// Implementation of IHierarchyScopeResolver for the Personnel module.
///
/// Resolves manager-subordinate hierarchies by traversing the ManagerId relationships.
/// Results for recursive subordinate lookups are cached per manager with a short TTL.
///
/// DESIGN: This lives in Personnel module because:
/// - Employee hierarchy is Personnel's domain
/// - Organization module only knows about structure (Company, Department, Position)
/// - Personnel owns the "who reports to whom" relationship
///
/// Cache:
/// - Key: personnel:{tenantId}:hierarchy:{managerId}
/// - TTL: 5 minutes
/// - Invalidated by ManagerChangedDomainEventHandler on any hierarchy mutation
/// - Background services (no tenant context) bypass the cache
///
/// Performance considerations:
/// - Uses recursive CTE for database traversal; for large organizations
///   consider replacing with a materialized closure table.
/// </summary>
public sealed class HierarchyScopeResolver : IHierarchyScopeResolver
{
    private const string CacheKeyPrefix = "personnel";
    private const string HierarchySegment = "hierarchy";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICache _cache;
    private readonly ITenantContext? _tenantContext;

    public HierarchyScopeResolver(
        IEmployeeRepository employeeRepository,
        ICache cache,
        ITenantContext? tenantContext = null)
    {
        _employeeRepository = employeeRepository;
        _cache = cache;
        _tenantContext = tenantContext;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Guid>> GetSubordinateIdsAsync(
        Guid managerId,
        bool includeIndirect = true,
        CancellationToken cancellationToken = default)
    {
        if (includeIndirect)
        {
            var cacheKey = BuildCacheKey(managerId);

            if (cacheKey is not null)
            {
                return await _cache.GetOrCreateAsync(
                    cacheKey,
                    () => _employeeRepository.GetAllSubordinateIdsAsync(managerId, cancellationToken),
                    CacheTtl,
                    cancellationToken);
            }

            // Background service (no tenant context) — skip cache
            return await _employeeRepository.GetAllSubordinateIdsAsync(managerId, cancellationToken);
        }

        // Direct reports only — not cached (infrequent, small result set)
        var directReports = await _employeeRepository.GetDirectReportsAsync(managerId, cancellationToken);
        var result = new List<Guid> { managerId };
        result.AddRange(directReports.Select(e => e.Id));
        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<Guid>> GetDirectSubordinateIdsAsync(
        Guid managerId,
        CancellationToken cancellationToken = default)
    {
        var directReports = await _employeeRepository.GetDirectReportsAsync(managerId, cancellationToken);
        return directReports.Select(e => e.Id).ToList();
    }

    /// <inheritdoc />
    public async Task<bool> IsSubordinateOfAsync(
        Guid employeeId,
        Guid managerId,
        CancellationToken cancellationToken = default)
    {
        if (employeeId == managerId)
            return false;

        return await _employeeRepository.IsSubordinateOfAsync(employeeId, managerId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> GetManagementChainAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        return await _employeeRepository.GetManagementChainAsync(employeeId, cancellationToken);
    }

    /// <summary>
    /// Returns the cache key prefix for all hierarchy entries of a given tenant.
    /// Used by <see cref="ManagerChangedDomainEventHandler"/> to bulk-invalidate stale entries.
    /// </summary>
    internal static string GetTenantHierarchyPrefix(Guid tenantId)
        => $"{CacheKeyPrefix}:{tenantId}:{HierarchySegment}:";

    private string? BuildCacheKey(Guid managerId)
    {
        var tenantId = _tenantContext?.TenantId;
        if (tenantId is null)
            return null;

        return $"{CacheKeyPrefix}:{tenantId}:{HierarchySegment}:{managerId}";
    }
}
