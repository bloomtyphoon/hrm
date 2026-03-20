using HRM.Modules.Attendance.Application.Abstractions;
using HRM.Modules.Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Attendance.Infrastructure.Persistence.Repositories;

internal sealed class LeaveApprovalSettingRepository : ILeaveApprovalSettingRepository
{
    private readonly AttendanceDbContext _context;

    public LeaveApprovalSettingRepository(AttendanceDbContext context) => _context = context;

    public async Task<LeaveApprovalSetting?> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => await _context.Set<LeaveApprovalSetting>()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId, cancellationToken);

    public void Add(LeaveApprovalSetting setting) => _context.Set<LeaveApprovalSetting>().Add(setting);
    public void Update(LeaveApprovalSetting setting) => _context.Set<LeaveApprovalSetting>().Update(setting);
}
