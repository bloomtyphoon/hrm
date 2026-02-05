using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Identity.Application.Commands.RegisterAccount;

/// <summary>
/// Command to register a new account (System or Employee).
/// Returns: Account ID on success, Error on failure (Result pattern).
/// </summary>
public sealed record RegisterAccountCommand(
    string Username,
    string Email,
    string Password,
    string FullName,
    string? PhoneNumber = null
) : IModuleCommand<Guid>
{
    public string ModuleName => "Identity";
}
