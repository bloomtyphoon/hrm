using HRM.Modules.Attendance.Domain.Entities;

namespace HRM.Modules.Attendance.Application.Abstractions;

public interface ILeaveApprovalStepRepository
{
    Task<List<LeaveApprovalStep>> GetByLeaveRequestIdAsync(Guid leaveRequestId, CancellationToken cancellationToken = default);
    Task<LeaveApprovalStep?> GetByRequestAndStepOrderAsync(Guid leaveRequestId, int stepOrder, CancellationToken cancellationToken = default);
    void Add(LeaveApprovalStep step);
    void Update(LeaveApprovalStep step);
}
