using HRM.Modules.Identity.Domain.Entities;

namespace HRM.Modules.Identity.Domain.Repositories;

/// <summary>
/// Repository interface for TenantScopeOverride aggregate.
/// </summary>
public interface ITenantScopeOverrideRepository
{
    Task<TenantScopeOverride?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get override for a specific permission action within the current tenant.
    /// </summary>
    Task<TenantScopeOverride?> GetByPermissionAsync(
        string module, string entity, string action,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all overrides for the current tenant.
    /// </summary>
    Task<List<TenantScopeOverride>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if an override exists for a specific permission action within the current tenant.
    /// </summary>
    Task<bool> ExistsAsync(
        string module, string entity, string action,
        CancellationToken cancellationToken = default);

    void Add(TenantScopeOverride entity);

    void Remove(TenantScopeOverride entity);
}
