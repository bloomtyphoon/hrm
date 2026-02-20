namespace HRM.Modules.Identity.Application.Queries.GetAccountRoles;

/// <summary>
/// DTO for an account's assigned role.
/// </summary>
public sealed record AccountRoleDto
{
    public required Guid RoleId { get; init; }
    public required string RoleName { get; init; }
    public string? RoleDescription { get; init; }
    public required bool IsSystemRole { get; init; }
    public Guid? CompanyId { get; init; }
    public required int PermissionCount { get; init; }
    public required DateTime AssignedAtUtc { get; init; }
    public Guid? AssignedById { get; init; }
}
