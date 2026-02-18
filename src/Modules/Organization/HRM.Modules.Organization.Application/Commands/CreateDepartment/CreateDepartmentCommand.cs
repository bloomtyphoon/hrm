using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Organization.Application.Commands.CreateDepartment;

/// <summary>
/// Command to create a new department within a company.
/// Supports hierarchical structure via optional ParentDepartmentId.
/// </summary>
public sealed record CreateDepartmentCommand(
    Guid CompanyId,
    string Code,
    string Name,
    Guid? ParentDepartmentId = null,
    Guid? ManagerId = null
) : IModuleCommand<Guid>
{
    public string ModuleName => "Organization";
}
