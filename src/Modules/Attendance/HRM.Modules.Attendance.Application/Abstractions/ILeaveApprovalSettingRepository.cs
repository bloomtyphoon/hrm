using HRM.Modules.Attendance.Domain.Entities;

namespace HRM.Modules.Attendance.Application.Abstractions;

public interface ILeaveApprovalSettingRepository
{
    Task<LeaveApprovalSetting?> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    void Add(LeaveApprovalSetting setting);
    void Update(LeaveApprovalSetting setting);
}
