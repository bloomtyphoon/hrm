using HRM.Modules.Attendance.Application.Abstractions;
using HRM.Modules.Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Attendance.Infrastructure.Persistence.Repositories;

internal sealed class LeaveTypeRepository : ILeaveTypeRepository
{
    private readonly AttendanceDbContext _context;

    public LeaveTypeRepository(AttendanceDbContext context) => _context = context;

    public async Task<LeaveType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Set<LeaveType>()
            .FirstOrDefaultAsync(lt => lt.Id == id, cancellationToken);

    public async Task<bool> ExistsByNameAsync(string name, Guid tenantId, Guid? excludeId = null, CancellationToken cancellationToken = default)
        => await _context.Set<LeaveType>()
            .AnyAsync(lt => lt.Name == name && lt.TenantId == tenantId && (!excludeId.HasValue || lt.Id != excludeId.Value), cancellationToken);

    public void Add(LeaveType leaveType) => _context.Set<LeaveType>().Add(leaveType);
    public void Update(LeaveType leaveType) => _context.Set<LeaveType>().Update(leaveType);
    public void Remove(LeaveType leaveType) => _context.Set<LeaveType>().Remove(leaveType);
}
