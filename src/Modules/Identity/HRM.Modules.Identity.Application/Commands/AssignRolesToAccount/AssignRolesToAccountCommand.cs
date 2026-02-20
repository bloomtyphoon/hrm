using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Identity.Application.Commands.AssignRolesToAccount;

/// <summary>
/// Command to assign one or more roles to an account.
/// </summary>
public sealed record AssignRolesToAccountCommand(
    Guid AccountId,
    List<Guid> RoleIds
) : IModuleCommand
{
    public string ModuleName => "Identity";
}
