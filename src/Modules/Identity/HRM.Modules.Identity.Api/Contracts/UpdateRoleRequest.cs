namespace HRM.Modules.Identity.Api.Contracts;

/// <summary>
/// Request DTO for updating an existing role.
/// </summary>
public sealed record UpdateRoleRequest(
    string Name,
    string? Description,
    List<PermissionRequest> Permissions
);
