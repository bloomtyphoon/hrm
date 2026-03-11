using HRM.Modules.Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Attendance.Application.Abstractions.Data;

/// <summary>
/// Read-only query context for Attendance module.
/// Used by query handlers to access AttendanceRecords with EF Core LINQ.
/// Automatically applies tenant and soft-delete global query filters.
/// </summary>
public interface IAttendanceQueryContext
{
    IQueryable<AttendanceRecord> AttendanceRecords { get; }
}
