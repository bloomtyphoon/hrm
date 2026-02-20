using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Identity.Application.Commands.ResetAccountPassword;

/// <summary>
/// Command for an admin to reset another account's password.
/// Does not require the current password — admin authority is sufficient.
/// </summary>
public sealed record ResetAccountPasswordCommand(
    Guid AccountId,
    string NewPassword
) : IModuleCommand
{
    public string ModuleName => "Identity";
}
