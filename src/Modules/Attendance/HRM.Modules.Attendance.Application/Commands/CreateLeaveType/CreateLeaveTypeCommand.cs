using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Attendance.Application.Commands.CreateLeaveType;

public sealed record CreateLeaveTypeCommand(
    string Name,
    int DefaultDaysPerYear,
    bool IsPaid = true,
    string? Description = null
) : IModuleCommand<Guid>
{
    public string ModuleName => "Attendance";
}
