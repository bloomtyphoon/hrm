using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Identity.Application.Commands.SuspendAccount;

/// <summary>
/// Command to suspend an active account.
/// </summary>
public sealed record SuspendAccountCommand(
    Guid AccountId
) : IModuleCommand
{
    public string ModuleName => "Identity";
}
