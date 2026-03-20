using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Attendance.Application.Commands.UpdateAttendance;

public sealed record UpdateAttendanceCommand(
    Guid RecordId,
    DateTime CheckInTimeUtc,
    DateTime? CheckOutTimeUtc = null,
    string? Notes = null
) : IModuleCommand
{
    public string ModuleName => "Attendance";
}
