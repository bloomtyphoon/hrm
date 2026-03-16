using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Security;

namespace HRM.Modules.Identity.Application.Commands.CreateTenantScopeOverride;

/// <summary>
/// Command to create a tenant scope override for a specific permission action.
/// Restricts which scopes are available for the current tenant.
/// </summary>
public sealed record CreateTenantScopeOverrideCommand(
    string Module,
    string Entity,
    string Action,
    List<DataScopeLevel> AllowedScopes,
    DataScopeLevel? DefaultScope = null
) : IModuleCommand<Guid>
{
    public string ModuleName => "Identity";
}
