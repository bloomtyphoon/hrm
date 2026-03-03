using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Attendance.Application.Commands.RecordManualAttendance;

/// <summary>
/// HR records attendance manually for an employee.
/// Requires Attendance.Record.ManualRecord permission (Company or Global scope).
/// </summary>
public sealed record RecordManualAttendanceCommand(
    Guid EmployeeId,
    DateOnly Date,
    DateTime CheckInTimeUtc,
    DateTime CheckOutTimeUtc,
    Guid? CompanyId = null,
    string? Notes = null
) : IModuleCommand<Guid>
{
    public string ModuleName => "Attendance";
}
