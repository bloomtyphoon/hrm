using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Personnel.Application.Commands.EndAssignment;

public sealed record EndAssignmentCommand(
    Guid EmployeeId,
    Guid AssignmentId,
    DateOnly EndDate
) : IModuleCommand
{
    public string ModuleName => "Personnel";
}
