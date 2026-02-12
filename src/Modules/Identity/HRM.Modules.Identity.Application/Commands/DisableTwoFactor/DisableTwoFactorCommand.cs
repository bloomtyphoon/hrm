using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Identity.Application.Commands.DisableTwoFactor;

/// <summary>
/// Command to disable two-factor authentication for an account.
/// </summary>
public sealed record DisableTwoFactorCommand(
    Guid AccountId
) : IModuleCommand
{
    public string ModuleName => "Identity";
}
