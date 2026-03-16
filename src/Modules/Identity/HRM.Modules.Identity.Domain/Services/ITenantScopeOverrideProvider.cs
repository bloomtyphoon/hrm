using HRM.Modules.Identity.Domain.Entities;

namespace HRM.Modules.Identity.Domain.Services;

/// <summary>
/// Provider for loading tenant scope overrides.
/// Abstracts DB access so PermissionCatalogService (Singleton) can load tenant data.
/// </summary>
public interface ITenantScopeOverrideProvider
{
    /// <summary>
    /// Get all scope overrides for a specific tenant.
    /// </summary>
    Task<List<TenantScopeOverride>> GetOverridesAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
