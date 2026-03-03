using HRM.Modules.Attendance.Domain.Entities;

namespace HRM.Modules.Attendance.Application.Abstractions;

/// <summary>
/// Repository for AttendanceRecord aggregate.
/// Write operations only — queries use IAttendanceQueryContext directly.
/// </summary>
public interface IAttendanceRecordRepository
{
    Task<AttendanceRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the active (not yet checked out) attendance record for the given employee on the given date.
    /// Returns null if no active check-in exists.
    /// </summary>
    Task<AttendanceRecord?> GetActiveCheckInAsync(
        Guid employeeId,
        DateOnly date,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if any attendance record exists for the given employee on the given date
    /// (regardless of status). Used to enforce one-record-per-day business rule.
    /// </summary>
    Task<bool> HasRecordForDateAsync(
        Guid employeeId,
        DateOnly date,
        CancellationToken cancellationToken = default);

    void Add(AttendanceRecord record);
    void Update(AttendanceRecord record);
}
