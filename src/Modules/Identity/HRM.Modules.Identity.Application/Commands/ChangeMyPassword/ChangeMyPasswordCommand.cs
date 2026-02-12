using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Identity.Application.Commands.ChangeMyPassword;

/// <summary>
/// Command for a user to change their own password.
/// Requires verification of the current password.
/// </summary>
public sealed record ChangeMyPasswordCommand(
    Guid AccountId,
    string CurrentPassword,
    string NewPassword
) : IModuleCommand
{
    public string ModuleName => "Identity";
}
