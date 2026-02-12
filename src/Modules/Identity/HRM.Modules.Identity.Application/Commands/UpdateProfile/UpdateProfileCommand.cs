using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Identity.Application.Commands.UpdateProfile;

/// <summary>
/// Command to update an account's profile information (fullName, phoneNumber).
/// </summary>
public sealed record UpdateProfileCommand(
    Guid AccountId,
    string FullName,
    string? PhoneNumber
) : IModuleCommand
{
    public string ModuleName => "Identity";
}
