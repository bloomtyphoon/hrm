using System.ComponentModel.DataAnnotations;

namespace HRM.Web.Models;

/// <summary>
/// Response model for role data (matches backend RoleSummaryDto / RoleResponse).
/// </summary>
public sealed class RoleResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public Guid? CompanyId { get; set; }
    public int PermissionCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }

    /// <summary>Computed display value: "System" or "Employee".</summary>
    public string RoleType => IsSystemRole ? "System" : "Employee";

    /// <summary>Roles from the API are active (soft-deleted ones are excluded).</summary>
    public bool IsActive => true;
}

/// <summary>
/// Detailed role response including permissions (matches backend RoleDetailDto / RoleResponse).
/// </summary>
public sealed class RoleDetailResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public Guid? CompanyId { get; set; }
    public List<RolePermissionResponse> Permissions { get; set; } = [];
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }

    /// <summary>Computed display value: "System" or "Employee".</summary>
    public string RoleType => IsSystemRole ? "System" : "Employee";

    /// <summary>Roles from the API are active (soft-deleted ones are excluded).</summary>
    public bool IsActive => true;
}

/// <summary>
/// Permission assigned to a role.
/// </summary>
public sealed class RolePermissionResponse
{
    public string Module { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Scope { get; set; }

    /// <summary>
    /// Full permission key: "Module.Entity.Action"
    /// </summary>
    public string PermissionKey => $"{Module}.{Entity}.{Action}";
}

/// <summary>
/// Request model for creating a role.
/// </summary>
public sealed class CreateRoleRequest
{
    [Required(ErrorMessage = "Role name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Role name must be between 2 and 100 characters")]
    [Display(Name = "Role Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Role type is required")]
    [Display(Name = "Role Type")]
    public string RoleType { get; set; } = "Employee"; // System or Employee

    /// <summary>
    /// Company ID for scoping the role. Auto-filled from CompanyContext.
    /// </summary>
    public Guid? CompanyId { get; set; }
}

/// <summary>
/// Request model for updating a role.
/// </summary>
public sealed class UpdateRoleRequest
{
    [Required(ErrorMessage = "Role name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Role name must be between 2 and 100 characters")]
    [Display(Name = "Role Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Request model for assigning permissions to a role.
/// </summary>
public sealed class AssignPermissionsRequest
{
    [Required(ErrorMessage = "At least one permission is required")]
    public List<PermissionAssignment> Permissions { get; set; } = [];
}

/// <summary>
/// Single permission assignment.
/// </summary>
public sealed class PermissionAssignment
{
    [Required]
    public string Module { get; set; } = string.Empty;

    [Required]
    public string Entity { get; set; } = string.Empty;

    [Required]
    public string Action { get; set; } = string.Empty;

    public string? Scope { get; set; }
}

/// <summary>
/// Request model for assigning roles to an account.
/// </summary>
public sealed class AssignRolesToAccountRequest
{
    [Required(ErrorMessage = "At least one role is required")]
    public List<Guid> RoleIds { get; set; } = [];
}

/// <summary>
/// View model for role list page.
/// </summary>
public sealed class RoleListViewModel
{
    public IReadOnlyList<RoleResponse> Roles { get; set; } = [];
    public string? SearchTerm { get; set; }
    public string? RoleTypeFilter { get; set; }
}

/// <summary>
/// View model for role detail/edit page.
/// </summary>
public sealed class RoleDetailViewModel
{
    public RoleDetailResponse Role { get; set; } = new();
    public PermissionCatalogResponse? PermissionCatalog { get; set; }
}
