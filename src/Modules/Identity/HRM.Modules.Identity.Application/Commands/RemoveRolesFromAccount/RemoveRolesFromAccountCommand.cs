using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Identity.Application.Commands.RemoveRolesFromAccount;

/// <summary>
/// Command to remove one or more roles from an account.
/// </summary>
public sealed record RemoveRolesFromAccountCommand(
    Guid AccountId,
    List<Guid> RoleIds
) : IModuleCommand
{
    public string ModuleName => "Identity";
}
