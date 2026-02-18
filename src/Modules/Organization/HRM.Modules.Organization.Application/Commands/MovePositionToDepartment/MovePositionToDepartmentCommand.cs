using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Organization.Application.Commands.MovePositionToDepartment;

public sealed record MovePositionToDepartmentCommand(
    Guid PositionId,
    Guid? DepartmentId
) : IModuleCommand
{
    public string ModuleName => "Organization";
}
