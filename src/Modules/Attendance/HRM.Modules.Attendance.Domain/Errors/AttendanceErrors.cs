using HRM.BuildingBlocks.Domain.Abstractions.Results;

namespace HRM.Modules.Attendance.Domain.Errors;

/// <summary>
/// Domain errors for Attendance module.
/// Convention: Code = "Attendance.{ErrorName}"
/// </summary>
public static class AttendanceErrors
{
    public static ConflictError AlreadyCheckedIn(Guid employeeId) =>
        new("Attendance.AlreadyCheckedIn",
            $"Employee '{employeeId}' is already checked in today.");

    public static NotFoundError ActiveCheckInNotFound(Guid employeeId) =>
        new("Attendance.ActiveCheckInNotFound",
            $"No active check-in found for employee '{employeeId}' today.");

    public static NotFoundError RecordNotFound(Guid id) =>
        new("Attendance.NotFound",
            $"Attendance record '{id}' was not found.");

    public static ValidationError EmployeeNotResolved() =>
        new("Attendance.EmployeeNotResolved",
            "Could not resolve employee from current user context. " +
            "Ensure your account has an employee profile linked.");

    public static ForbiddenError ManualRecordForbidden(Guid employeeId) =>
        new("Attendance.ManualRecord.Forbidden",
            $"You don't have permission to record attendance for employee '{employeeId}'. " +
            "The employee is outside your company scope.");
}
