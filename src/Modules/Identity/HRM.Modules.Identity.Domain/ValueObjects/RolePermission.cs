using HRM.BuildingBlocks.Domain.Abstractions.Security;

namespace HRM.Modules.Identity.Domain.ValueObjects;

/// <summary>
/// Value object representing a permission assigned to a role.
///
/// Structure:
/// - Module.Entity.Action format (e.g., "Personnel.Employee.View")
/// - Optional Scope for data visibility
/// - Immutable once created
/// </summary>
public sealed record RolePermission
{
    /// <summary>Module name from Permission Catalog.</summary>
    public string Module { get; }

    /// <summary>Entity name from Permission Catalog.</summary>
    public string Entity { get; }

    /// <summary>Action name from Permission Catalog.</summary>
    public string Action { get; }

    /// <summary>
    /// Optional scope for data visibility.
    /// NULL = No scope restriction (for operators or actions without scopes).
    /// </summary>
    public DataScopeLevel? Scope { get; }

    /// <summary>Full permission identifier: "Module.Entity.Action".</summary>
    public string PermissionKey => $"{Module}.{Entity}.{Action}";

    private RolePermission(string module, string entity, string action, DataScopeLevel? scope)
    {
        Module = module;
        Entity = entity;
        Action = action;
        Scope = scope;
    }

    /// <summary>Factory method to create a RolePermission with validation.</summary>
    public static RolePermission Create(string module, string entity, string action, DataScopeLevel? scope = null)
    {
        if (string.IsNullOrWhiteSpace(module))
            throw new ArgumentException("Module cannot be null or empty", nameof(module));
        if (string.IsNullOrWhiteSpace(entity))
            throw new ArgumentException("Entity cannot be null or empty", nameof(entity));
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action cannot be null or empty", nameof(action));

        return new RolePermission(module, entity, action, scope);
    }

    /// <summary>Check if this permission has a scope restriction.</summary>
    public bool HasScope() => Scope is not null;

    /// <summary>Get scope display name for UI.</summary>
    public string GetScopeDisplay()
    {
        if (Scope is null) return "No Scope";
        return Scope.Name;
    }

    /// <summary>Format permission for display: "Module.Entity.Action (Scope)".</summary>
    public override string ToString()
    {
        return HasScope() ? $"{PermissionKey} ({GetScopeDisplay()})" : PermissionKey;
    }
}
