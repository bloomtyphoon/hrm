using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Security;

namespace HRM.Modules.Identity.Application.Commands.UpdateTenantScopeOverride;

/// <summary>
/// Command to update an existing tenant scope override.
/// </summary>
public sealed record UpdateTenantScopeOverrideCommand(
    Guid Id,
    List<DataScopeLevel> AllowedScopes,
    DataScopeLevel? DefaultScope = null
) : IModuleCommand
{
    public string ModuleName => "Identity";
}
