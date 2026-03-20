using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.Modules.Attendance.Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Attendance.Application.Queries.GetLeaveApprovalSteps;

internal sealed class GetLeaveApprovalStepsQueryHandler
    : IQueryHandler<GetLeaveApprovalStepsQuery, IReadOnlyList<LeaveApprovalStepDto>>
{
    private readonly IAttendanceQueryContext _context;

    public GetLeaveApprovalStepsQueryHandler(IAttendanceQueryContext context) => _context = context;

    public async Task<IReadOnlyList<LeaveApprovalStepDto>> Handle(
        GetLeaveApprovalStepsQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.LeaveApprovalSteps
            .AsNoTracking()
            .Where(s => s.LeaveRequestId == request.LeaveRequestId)
            .OrderBy(s => s.StepOrder)
            .Select(s => new LeaveApprovalStepDto
            {
                Id = s.Id,
                StepOrder = s.StepOrder,
                ApproverEmployeeId = s.ApproverEmployeeId,
                ApprovalLevelName = s.ApprovalLevelName,
                Status = s.Status.ToString(),
                DecisionDateUtc = s.DecisionDateUtc,
                Notes = s.Notes
            })
            .ToListAsync(cancellationToken);
    }
}
