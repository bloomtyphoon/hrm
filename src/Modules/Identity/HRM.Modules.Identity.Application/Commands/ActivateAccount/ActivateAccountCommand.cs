using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Identity.Application.Commands.ActivateAccount;

/// <summary>
/// Command to activate a pending account.
/// </summary>
public sealed record ActivateAccountCommand(
    Guid AccountId
) : ICommand;
