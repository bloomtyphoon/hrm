using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Identity.Application.Commands.RevokeSuperAdmin;

/// <summary>
/// Command to revoke super admin privileges from a system account.
/// </summary>
public sealed record RevokeSuperAdminCommand(
    Guid AccountId
) : IModuleCommand
{
    public string ModuleName => "Identity";
}
