using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Organization.Application.Commands.UpdateCompany;

public sealed record UpdateCompanyCommand(
    Guid CompanyId,
    string Name,
    string? TaxId = null
) : IModuleCommand
{
    public string ModuleName => "Organization";
}
