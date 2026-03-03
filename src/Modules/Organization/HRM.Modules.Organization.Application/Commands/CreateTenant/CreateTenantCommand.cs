using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Organization.Application.Commands.CreateTenant;

public sealed record CreateTenantCommand(string Code, string Name) : IModuleCommand<Guid>
{
    public string ModuleName => "Organization";
}
