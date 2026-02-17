using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Personnel.Application.Commands.AddAssignment;

/// <summary>
/// Command to add an assignment (company/department/position) to an employee.
/// Uses weak references to Organization module entities.
/// </summary>
public sealed record AddAssignmentCommand(
    Guid EmployeeId,
    Guid CompanyId,
    Guid DepartmentId,
    Guid PositionId,
    DateOnly StartDate,
    bool IsPrimary = false
) : IModuleCommand<Guid>
{
    public string ModuleName => "Personnel";
}
