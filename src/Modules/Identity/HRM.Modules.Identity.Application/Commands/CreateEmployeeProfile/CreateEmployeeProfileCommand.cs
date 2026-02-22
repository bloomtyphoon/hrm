using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Security;

namespace HRM.Modules.Identity.Application.Commands.CreateEmployeeProfile;

/// <summary>
/// Command to create an employee profile linking an employee account to an employee entity.
/// </summary>
public sealed record CreateEmployeeProfileCommand(
    Guid AccountId,
    Guid EmployeeId,
    DataScopeLevel DefaultScopeLevel = DataScopeLevel.Self,
    Guid? PrimaryCompanyId = null,
    Guid? PrimaryDepartmentId = null,
    Guid? PrimaryPositionId = null,
    IReadOnlyList<Guid>? CompanyIds = null
) : IModuleCommand<Guid>
{
    public string ModuleName => "Identity";
}
