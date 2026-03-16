using HRM.BuildingBlocks.Domain.Abstractions.Multitenancy;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.BuildingBlocks.Domain.Entities;

namespace HRM.Modules.Identity.Domain.Entities;

/// <summary>
/// Per-tenant override for available scopes on a specific permission action.
///
/// The base permission catalog (XML) defines default available scopes for each action.
/// Tenants can override which scopes are allowed for their users via this entity.
///
/// Example: Tenant A wants to restrict Employee.View to only Company and Self scopes,
/// removing Department scope that the base catalog allows.
///
/// Design:
/// - One row per (Tenant, Module, Entity, Action) combination
/// - AllowedScopes replaces the XML-defined scopes entirely
/// - If no override exists, the base catalog scopes apply
/// </summary>
public class TenantScopeOverride : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }

    /// <summary>
    /// Permission module name (e.g., "Personnel", "Identity")
    /// </summary>
    public string Module { get; private set; } = default!;

    /// <summary>
    /// Permission entity name (e.g., "Employee", "Account")
    /// </summary>
    public string Entity { get; private set; } = default!;

    /// <summary>
    /// Permission action name (e.g., "View", "Create", "Update")
    /// </summary>
    public string Action { get; private set; } = default!;

    /// <summary>
    /// Allowed scope levels for this action (replaces XML catalog scopes).
    /// Stored as JSON array of DataScopeLevel values.
    /// </summary>
    public List<DataScopeLevel> AllowedScopes { get; private set; } = new();

    /// <summary>
    /// Default scope to pre-select in UI (optional).
    /// Must be one of the AllowedScopes values.
    /// </summary>
    public DataScopeLevel? DefaultScope { get; private set; }

    private TenantScopeOverride() { }

    /// <summary>
    /// Create a new tenant scope override.
    /// </summary>
    public static TenantScopeOverride Create(
        Guid tenantId,
        string module,
        string entity,
        string action,
        List<DataScopeLevel> allowedScopes,
        DataScopeLevel? defaultScope = null)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(module))
            throw new ArgumentException("Module is required.", nameof(module));
        if (string.IsNullOrWhiteSpace(entity))
            throw new ArgumentException("Entity is required.", nameof(entity));
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action is required.", nameof(action));
        if (allowedScopes is null || allowedScopes.Count == 0)
            throw new ArgumentException("At least one allowed scope is required.", nameof(allowedScopes));
        if (defaultScope.HasValue && !allowedScopes.Contains(defaultScope.Value))
            throw new ArgumentException("Default scope must be one of the allowed scopes.", nameof(defaultScope));

        return new TenantScopeOverride
        {
            TenantId = tenantId,
            Module = module,
            Entity = entity,
            Action = action,
            AllowedScopes = new List<DataScopeLevel>(allowedScopes),
            DefaultScope = defaultScope
        };
    }

    /// <summary>
    /// Update the allowed scopes and default scope.
    /// </summary>
    public void Update(List<DataScopeLevel> allowedScopes, DataScopeLevel? defaultScope = null)
    {
        if (allowedScopes is null || allowedScopes.Count == 0)
            throw new ArgumentException("At least one allowed scope is required.", nameof(allowedScopes));
        if (defaultScope.HasValue && !allowedScopes.Contains(defaultScope.Value))
            throw new ArgumentException("Default scope must be one of the allowed scopes.", nameof(defaultScope));

        AllowedScopes = new List<DataScopeLevel>(allowedScopes);
        DefaultScope = defaultScope;
        MarkAsModified();
    }

    /// <summary>
    /// Permission key in "Module.Entity.Action" format.
    /// </summary>
    public string PermissionKey => $"{Module}.{Entity}.{Action}";
}
