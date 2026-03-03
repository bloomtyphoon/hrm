namespace HRM.Modules.Attendance.Domain.Entities;

/// <summary>
/// Attendance record workflow states.
/// Only tracks the workflow state, not how the record was created.
/// Use <see cref="AttendanceRecord.IsManualEntry"/> to distinguish HR-created records.
/// </summary>
public enum AttendanceStatus
{
    /// <summary>Employee is currently checked in (no checkout yet).</summary>
    CheckedIn = 1,

    /// <summary>Employee has checked out. Record is complete.</summary>
    CheckedOut = 2
}
