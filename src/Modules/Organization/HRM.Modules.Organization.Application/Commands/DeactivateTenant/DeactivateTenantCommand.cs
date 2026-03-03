using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Organization.Application.Commands.DeactivateTenant;

public sealed record DeactivateTenantCommand(Guid TenantId) : IModuleCommand
{
    public string ModuleName => "Organization";
}
