using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Organization.Application.Commands.UpdateTenant;

public sealed record UpdateTenantCommand(Guid TenantId, string Name, string? Subdomain = null) : IModuleCommand
{
    public string ModuleName => "Organization";
}
