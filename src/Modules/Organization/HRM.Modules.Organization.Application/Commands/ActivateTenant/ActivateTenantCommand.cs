using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Organization.Application.Commands.ActivateTenant;

public sealed record ActivateTenantCommand(Guid TenantId) : IModuleCommand
{
    public string ModuleName => "Organization";
}
