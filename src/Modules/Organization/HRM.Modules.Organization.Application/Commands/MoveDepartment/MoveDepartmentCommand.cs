using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Organization.Application.Commands.MoveDepartment;

public sealed record MoveDepartmentCommand(
    Guid DepartmentId,
    Guid? NewParentDepartmentId
) : IModuleCommand
{
    public string ModuleName => "Organization";
}
