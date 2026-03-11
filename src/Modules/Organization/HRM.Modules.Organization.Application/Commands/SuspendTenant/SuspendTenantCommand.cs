using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Organization.Application.Commands.SuspendTenant;

public sealed record SuspendTenantCommand(Guid TenantId) : IModuleCommand
{
    public string ModuleName => "Organization";
}
