using HRM.Modules.Attendance.Domain.Entities;

namespace HRM.Modules.Attendance.Application.Abstractions;

public interface ILeaveTypeRepository
{
    Task<LeaveType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, Guid tenantId, Guid? excludeId = null, CancellationToken cancellationToken = default);
    void Add(LeaveType leaveType);
    void Update(LeaveType leaveType);
    void Remove(LeaveType leaveType);
}
