using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Identity.Application.Commands.CreateSystemProfile;

/// <summary>
/// Command to create a system profile for a system account.
/// </summary>
public sealed record CreateSystemProfileCommand(
    Guid AccountId,
    bool IsSuperAdmin = false,
    string? Department = null,
    string? JobTitle = null
) : IModuleCommand<Guid>
{
    public string ModuleName => "Identity";
}
