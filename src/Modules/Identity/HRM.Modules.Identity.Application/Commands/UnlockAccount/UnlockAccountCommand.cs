using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Identity.Application.Commands.UnlockAccount;

/// <summary>
/// Command to forcefully unlock a locked account (admin operation).
/// Resets failed login attempts and clears lockout timestamp.
/// </summary>
public sealed record UnlockAccountCommand(
    Guid AccountId
) : IModuleCommand
{
    public string ModuleName => "Identity";
}
