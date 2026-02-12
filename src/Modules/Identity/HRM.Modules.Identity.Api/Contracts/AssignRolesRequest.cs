namespace HRM.Modules.Identity.Api.Contracts;

/// <summary>
/// Request DTO for assigning/removing roles to/from an account.
/// </summary>
public sealed record AssignRolesRequest(
    List<Guid> RoleIds
);
