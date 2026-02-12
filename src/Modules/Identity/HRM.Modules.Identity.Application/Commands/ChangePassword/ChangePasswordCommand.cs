using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Identity.Application.Commands.ChangePassword;

/// <summary>
/// Command to change an account's password.
/// Requires current password verification for self-change.
/// Admin can reset without current password by setting IsAdminReset = true.
/// </summary>
public sealed record ChangePasswordCommand(
    Guid AccountId,
    string? CurrentPassword,
    string NewPassword,
    bool IsAdminReset = false
) : IModuleCommand
{
    public string ModuleName => "Identity";
}
