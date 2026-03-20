using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Attendance.Application.Commands.UpdateShift;

public sealed record UpdateShiftCommand(
    Guid ShiftId,
    string Name,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? Description = null
) : IModuleCommand
{
    public string ModuleName => "Attendance";
}
