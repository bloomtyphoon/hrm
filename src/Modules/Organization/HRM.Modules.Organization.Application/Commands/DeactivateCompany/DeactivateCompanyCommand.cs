using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Organization.Application.Commands.DeactivateCompany;

public sealed record DeactivateCompanyCommand(Guid CompanyId) : IModuleCommand
{
    public string ModuleName => "Organization";
}
