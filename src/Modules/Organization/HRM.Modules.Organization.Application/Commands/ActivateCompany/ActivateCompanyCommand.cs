using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Organization.Application.Commands.ActivateCompany;

public sealed record ActivateCompanyCommand(Guid CompanyId) : IModuleCommand
{
    public string ModuleName => "Organization";
}
