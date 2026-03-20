using HRM.Modules.Attendance.Application.Abstractions;
using HRM.Modules.Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Attendance.Infrastructure.Persistence.Repositories;

internal sealed class LeaveApprovalStepRepository : ILeaveApprovalStepRepository
{
    private readonly AttendanceDbContext _context;

    public LeaveApprovalStepRepository(AttendanceDbContext context) => _context = context;

    public async Task<List<LeaveApprovalStep>> GetByLeaveRequestIdAsync(Guid leaveRequestId, CancellationToken cancellationToken = default)
        => await _context.Set<LeaveApprovalStep>()
            .Where(s => s.LeaveRequestId == leaveRequestId)
            .OrderBy(s => s.StepOrder)
            .ToListAsync(cancellationToken);

    public async Task<LeaveApprovalStep?> GetByRequestAndStepOrderAsync(Guid leaveRequestId, int stepOrder, CancellationToken cancellationToken = default)
        => await _context.Set<LeaveApprovalStep>()
            .FirstOrDefaultAsync(s => s.LeaveRequestId == leaveRequestId && s.StepOrder == stepOrder, cancellationToken);

    public void Add(LeaveApprovalStep step) => _context.Set<LeaveApprovalStep>().Add(step);
    public void Update(LeaveApprovalStep step) => _context.Set<LeaveApprovalStep>().Update(step);
}
