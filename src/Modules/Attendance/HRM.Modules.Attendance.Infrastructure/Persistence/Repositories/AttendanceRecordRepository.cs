using HRM.Modules.Attendance.Application.Abstractions;
using HRM.Modules.Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Attendance.Infrastructure.Persistence.Repositories;

internal sealed class AttendanceRecordRepository : IAttendanceRecordRepository
{
    private readonly AttendanceDbContext _context;

    public AttendanceRecordRepository(AttendanceDbContext context)
    {
        _context = context;
    }

    public async Task<AttendanceRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Set<AttendanceRecord>()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<AttendanceRecord?> GetActiveCheckInAsync(
        Guid employeeId,
        DateOnly date,
        CancellationToken cancellationToken = default)
        => await _context.Set<AttendanceRecord>()
            .FirstOrDefaultAsync(r =>
                r.EmployeeId == employeeId &&
                r.Date == date &&
                r.Status == AttendanceStatus.CheckedIn,
                cancellationToken);

    public async Task<bool> HasRecordForDateAsync(
        Guid employeeId,
        DateOnly date,
        CancellationToken cancellationToken = default)
        => await _context.Set<AttendanceRecord>()
            .AnyAsync(r => r.EmployeeId == employeeId && r.Date == date, cancellationToken);

    public void Add(AttendanceRecord record)
        => _context.Set<AttendanceRecord>().Add(record);

    public void Update(AttendanceRecord record)
        => _context.Set<AttendanceRecord>().Update(record);

    public void Remove(AttendanceRecord record)
        => _context.Set<AttendanceRecord>().Remove(record);
}
