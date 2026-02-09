using HRM.BuildingBlocks.Domain.Abstractions.Security;

namespace HRM.Modules.Identity.Api.Contracts;

/// <summary>
/// Request DTO for creating a new role.
/// </summary>
public sealed record CreateRoleRequest(
    string Name,
    string? Description,
    bool IsSystemRole,
    List<PermissionRequest> Permissions
);

/// <summary>
/// Request DTO for a single permission.
/// </summary>
public sealed record PermissionRequest(
    string Module,
    string Entity,
    string Action,
    DataScopeLevel? Scope = null
);
