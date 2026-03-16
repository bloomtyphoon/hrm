using HRM.Modules.Identity.Domain.ValueObjects;

namespace HRM.Modules.Identity.Domain.Services;

/// <summary>
/// Service interface for loading permission catalog.
///
/// Two-tier catalog:
/// - Base catalog (XML): Global schema definition shared by all tenants
/// - Tenant overrides (DB): Per-tenant scope restrictions applied on top of base
///
/// Methods without tenantId return the base catalog.
/// Methods with tenantId merge base catalog with tenant overrides.
/// </summary>
public interface IPermissionCatalogService
{
    /// <summary>
    /// Load the base catalog (XML only, no tenant overrides).
    /// </summary>
    Task<List<PermissionModule>> LoadBaseCatalogAsync();

    /// <summary>
    /// Load catalog with tenant-specific scope overrides applied.
    /// Falls back to base catalog for actions without overrides.
    /// </summary>
    Task<List<PermissionModule>> LoadCatalogAsync(Guid tenantId);

    /// <summary>
    /// Load catalog - delegates to LoadBaseCatalogAsync for backward compatibility.
    /// Prefer LoadCatalogAsync(tenantId) when tenant context is available.
    /// </summary>
    Task<List<PermissionModule>> LoadCatalogAsync();

    /// <summary>
    /// Get specific module from base catalog by name.
    /// </summary>
    Task<PermissionModule?> GetModuleAsync(string moduleName);

    /// <summary>
    /// Get specific entity from base catalog.
    /// </summary>
    Task<PermissionEntity?> GetEntityAsync(string moduleName, string entityName);

    /// <summary>
    /// Get specific action from base catalog.
    /// </summary>
    Task<PermissionAction?> GetActionAsync(string moduleName, string entityName, string actionName);

    /// <summary>
    /// Get specific action with tenant overrides applied.
    /// </summary>
    Task<PermissionAction?> GetActionAsync(Guid tenantId, string moduleName, string entityName, string actionName);

    /// <summary>
    /// Check if permission exists in base catalog.
    /// </summary>
    Task<bool> ExistsAsync(string moduleName, string entityName, string actionName);
}
