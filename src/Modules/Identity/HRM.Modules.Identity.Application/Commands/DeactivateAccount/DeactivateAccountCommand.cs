using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Identity.Application.Commands.DeactivateAccount;

/// <summary>
/// Command to deactivate an account.
/// </summary>
public sealed record DeactivateAccountCommand(
    Guid AccountId
) : IModuleCommand
{
    public string ModuleName => "Identity";
}
