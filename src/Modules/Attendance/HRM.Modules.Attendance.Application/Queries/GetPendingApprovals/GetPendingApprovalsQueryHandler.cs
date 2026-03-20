using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Application.Pagination;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Attendance.Application.Abstractions.Data;
using HRM.Modules.Attendance.Application.Security;
using HRM.Modules.Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Attendance.Application.Queries.GetPendingApprovals;

internal sealed class GetPendingApprovalsQueryHandler
    : IQueryHandler<GetPendingApprovalsQuery, PagedResult<PendingApprovalDto>>
{
    private readonly IAttendanceQueryContext _context;
    private readonly IDataScopeService _dataScopeService;
    private readonly IExecutionContext _executionContext;

    public GetPendingApprovalsQueryHandler(
        IAttendanceQueryContext context,
        IDataScopeService dataScopeService,
        IExecutionContext executionContext)
    {
        _context = context;
        _dataScopeService = dataScopeService;
        _executionContext = executionContext;
    }

    public async Task<PagedResult<PendingApprovalDto>> Handle(
        GetPendingApprovalsQuery request,
        CancellationToken cancellationToken)
    {
        var rule = await _dataScopeService.GetScopeRuleAsync(
            _executionContext.UserId, AttendancePermissions.Leave.Approve, cancellationToken);

        if (rule.SelfEmployeeId is null)
            return PagedResult<PendingApprovalDto>.Empty(request.PageNumber, request.PageSize);

        var approverEmployeeId = rule.SelfEmployeeId.Value;

        // Find leave requests where:
        // 1. Status is Pending
        // 2. There is an approval step for this approver at the current step
        var query = from lr in _context.LeaveRequests.AsNoTracking()
                    join step in _context.LeaveApprovalSteps.AsNoTracking()
                        on new { LeaveRequestId = lr.Id, StepOrder = lr.CurrentApprovalStep!.Value }
                        equals new { step.LeaveRequestId, step.StepOrder }
                    join lt in _context.LeaveTypes.AsNoTracking()
                        on lr.LeaveTypeId equals lt.Id
                    where lr.Status == LeaveStatus.Pending
                        && lr.CurrentApprovalStep.HasValue
                        && step.ApproverEmployeeId == approverEmployeeId
                        && step.Status == LeaveApprovalStepStatus.Pending
                    orderby lr.StartDate
                    select new PendingApprovalDto
                    {
                        LeaveRequestId = lr.Id,
                        EmployeeId = lr.EmployeeId,
                        LeaveTypeId = lr.LeaveTypeId,
                        LeaveTypeName = lt.Name,
                        StartDate = lr.StartDate,
                        EndDate = lr.EndDate,
                        TotalDays = lr.EndDate.DayNumber - lr.StartDate.DayNumber + 1,
                        Reason = lr.Reason,
                        CurrentStep = lr.CurrentApprovalStep!.Value,
                        TotalSteps = lr.TotalApprovalSteps!.Value,
                        ApprovalLevelName = step.ApprovalLevelName
                    };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<PendingApprovalDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
