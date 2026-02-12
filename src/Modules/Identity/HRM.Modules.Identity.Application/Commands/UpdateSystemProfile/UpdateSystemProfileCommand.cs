using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Identity.Application.Commands.UpdateSystemProfile;

/// <summary>
/// Command to update a system profile's information.
/// </summary>
public sealed record UpdateSystemProfileCommand(
    Guid AccountId,
    string? Department,
    string? JobTitle,
    string? Notes
) : IModuleCommand
{
    public string ModuleName => "Identity";
}
