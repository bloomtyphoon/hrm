using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Identity.Application.Commands.DeleteRole;

/// <summary>
/// Command to soft-delete a role.
/// </summary>
public sealed record DeleteRoleCommand(
    Guid RoleId
) : IModuleCommand
{
    public string ModuleName => "Identity";
}
