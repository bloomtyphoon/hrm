using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Organization.Application.Commands.UpdateDepartment;

public sealed record UpdateDepartmentCommand(
    Guid DepartmentId,
    string Name,
    Guid? ManagerId = null
) : IModuleCommand
{
    public string ModuleName => "Organization";
}
