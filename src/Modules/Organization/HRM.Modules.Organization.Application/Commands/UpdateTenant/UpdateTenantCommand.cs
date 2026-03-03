using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Organization.Application.Commands.UpdateTenant;

public sealed record UpdateTenantCommand(Guid TenantId, string Name) : IModuleCommand
{
    public string ModuleName => "Organization";
}
