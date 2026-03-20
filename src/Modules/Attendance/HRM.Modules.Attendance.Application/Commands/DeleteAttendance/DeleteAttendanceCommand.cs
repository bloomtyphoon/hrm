using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Attendance.Application.Commands.DeleteAttendance;

public sealed record DeleteAttendanceCommand(
    Guid RecordId
) : IModuleCommand
{
    public string ModuleName => "Attendance";
}
