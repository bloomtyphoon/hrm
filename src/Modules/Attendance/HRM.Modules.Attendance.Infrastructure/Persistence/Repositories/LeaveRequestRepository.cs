using HRM.Modules.Attendance.Application.Abstractions;
using HRM.Modules.Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Attendance.Infrastructure.Persistence.Repositories;

internal sealed class LeaveRequestRepository : ILeaveRequestRepository
{
    private readonly AttendanceDbContext _context;

    public LeaveRequestRepository(AttendanceDbContext context) => _context = context;

    public async Task<LeaveRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Set<LeaveRequest>()
            .FirstOrDefaultAsync(lr => lr.Id == id, cancellationToken);

    public void Add(LeaveRequest leaveRequest) => _context.Set<LeaveRequest>().Add(leaveRequest);
    public void Update(LeaveRequest leaveRequest) => _context.Set<LeaveRequest>().Update(leaveRequest);
}
