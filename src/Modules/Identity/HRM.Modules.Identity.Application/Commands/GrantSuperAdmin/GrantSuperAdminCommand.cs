using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Identity.Application.Commands.GrantSuperAdmin;

/// <summary>
/// Command to grant super admin privileges to a system account.
/// </summary>
public sealed record GrantSuperAdminCommand(
    Guid AccountId
) : IModuleCommand
{
    public string ModuleName => "Identity";
}
