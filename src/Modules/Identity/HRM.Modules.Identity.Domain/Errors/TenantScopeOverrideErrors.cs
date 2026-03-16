using HRM.BuildingBlocks.Domain.Abstractions.Results;

namespace HRM.Modules.Identity.Domain.Errors;

/// <summary>
/// Domain errors for TenantScopeOverride entity operations.
/// </summary>
public static class TenantScopeOverrideErrors
{
    public static NotFoundError NotFound(Guid id) =>
        new("TenantScopeOverride.NotFound", $"Tenant scope override with ID '{id}' was not found.");

    public static ConflictError AlreadyExists(string module, string entity, string action) =>
        new("TenantScopeOverride.AlreadyExists",
            $"A scope override for '{module}.{entity}.{action}' already exists for this tenant.");

    public static ValidationError PermissionNotInCatalog(string module, string entity, string action) =>
        new("TenantScopeOverride.PermissionNotInCatalog",
            $"Permission '{module}.{entity}.{action}' does not exist in the Permission Catalog.");

    public static ValidationError ScopeNotInCatalog(string permissionKey, string scope) =>
        new("TenantScopeOverride.ScopeNotInCatalog",
            $"Scope '{scope}' is not available for permission '{permissionKey}' in the base catalog. " +
            "Tenant overrides can only restrict scopes, not add new ones.");
}
