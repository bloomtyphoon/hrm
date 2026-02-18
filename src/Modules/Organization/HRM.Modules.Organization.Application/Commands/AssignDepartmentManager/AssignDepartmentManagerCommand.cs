using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Organization.Application.Commands.AssignDepartmentManager;

public sealed record AssignDepartmentManagerCommand(
    Guid DepartmentId,
    Guid ManagerId
) : IModuleCommand
{
    public string ModuleName => "Organization";
}
