using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Security;

namespace HRM.Modules.Identity.Application.Commands.UpdateEmployeeProfile;

/// <summary>
/// Command to update an employee profile's assignments and scope.
/// </summary>
public sealed record UpdateEmployeeProfileCommand(
    Guid AccountId,
    Guid? PrimaryCompanyId,
    Guid? PrimaryDepartmentId,
    Guid? PrimaryPositionId,
    DataScopeLevel DefaultScopeLevel,
    bool CanAccessAllAssignedCompanies
) : IModuleCommand
{
    public string ModuleName => "Identity";
}
