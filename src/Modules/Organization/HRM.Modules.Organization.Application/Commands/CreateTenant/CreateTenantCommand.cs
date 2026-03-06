using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Organization.Application.Commands.CreateTenant;

public sealed record CreateTenantCommand(string Code, string Name, string? Subdomain = null) : IModuleCommand<Guid>
{
    public string ModuleName => "Organization";
}
