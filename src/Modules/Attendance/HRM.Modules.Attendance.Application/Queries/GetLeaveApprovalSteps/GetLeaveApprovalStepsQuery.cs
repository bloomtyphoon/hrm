using HRM.BuildingBlocks.Application.Abstractions.Queries;

namespace HRM.Modules.Attendance.Application.Queries.GetLeaveApprovalSteps;

public sealed record GetLeaveApprovalStepsQuery(Guid LeaveRequestId)
    : IQuery<IReadOnlyList<LeaveApprovalStepDto>>;
