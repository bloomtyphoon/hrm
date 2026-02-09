using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.Modules.Identity.Application.Commands.CreateRole;

namespace HRM.Modules.Identity.Application.Commands.UpdateRole;

/// <summary>
/// Command to update an existing role's name, description, and permissions.
/// </summary>
public sealed record UpdateRoleCommand(
    Guid RoleId,
    string Name,
    string? Description,
    List<PermissionDto> Permissions
) : IModuleCommand
{
    public string ModuleName => "Identity";
}
