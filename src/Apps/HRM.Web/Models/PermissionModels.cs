namespace HRM.Web.Models;

/// <summary>
/// Permission catalog containing all available permissions in the system.
/// </summary>
public sealed class PermissionCatalogResponse
{
    public List<PermissionModuleResponse> Modules { get; set; } = [];
}

/// <summary>
/// A module in the permission catalog (e.g., Identity, Personnel, Attendance).
/// </summary>
public sealed class PermissionModuleResponse
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<PermissionEntityResponse> Entities { get; set; } = [];
}

/// <summary>
/// An entity within a module (e.g., Account, Role, Employee).
/// </summary>
public sealed class PermissionEntityResponse
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<PermissionActionResponse> Actions { get; set; } = [];
}

/// <summary>
/// An action that can be performed on an entity (e.g., View, Create, Update, Delete).
/// </summary>
public sealed class PermissionActionResponse
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> AvailableScopes { get; set; } = [];
}

/// <summary>
/// User's effective permissions.
/// </summary>
public sealed class UserPermissionsResponse
{
    public Guid AccountId { get; set; }
    public List<string> Permissions { get; set; } = [];
    public List<string> Roles { get; set; } = [];
    public bool IsSuperAdmin { get; set; }
}

/// <summary>
/// View model for permission catalog page.
/// </summary>
public sealed class PermissionCatalogViewModel
{
    public PermissionCatalogResponse Catalog { get; set; } = new();
}
