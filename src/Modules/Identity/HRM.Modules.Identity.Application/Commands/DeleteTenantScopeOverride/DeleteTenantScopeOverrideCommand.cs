using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Identity.Application.Commands.DeleteTenantScopeOverride;

/// <summary>
/// Command to delete a tenant scope override, reverting to base catalog scopes.
/// </summary>
public sealed record DeleteTenantScopeOverrideCommand(Guid Id) : IModuleCommand
{
    public string ModuleName => "Identity";
}
