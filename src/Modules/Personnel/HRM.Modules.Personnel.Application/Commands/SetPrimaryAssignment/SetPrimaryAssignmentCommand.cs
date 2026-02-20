using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Personnel.Application.Commands.SetPrimaryAssignment;

public sealed record SetPrimaryAssignmentCommand(
    Guid EmployeeId,
    Guid AssignmentId
) : IModuleCommand
{
    public string ModuleName => "Personnel";
}
