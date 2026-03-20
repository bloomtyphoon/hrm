using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Attendance.Application.Abstractions;
using HRM.Modules.Attendance.Application.Security;
using HRM.Modules.Attendance.Domain.Errors;

namespace HRM.Modules.Attendance.Application.Commands.CancelLeaveRequest;

internal sealed class CancelLeaveRequestCommandHandler
    : ICommandHandler<CancelLeaveRequestCommand>
{
    private readonly ILeaveRequestRepository _repository;
    private readonly IDataScopeService _dataScopeService;
    private readonly IExecutionContext _executionContext;

    public CancelLeaveRequestCommandHandler(
        ILeaveRequestRepository repository,
        IDataScopeService dataScopeService,
        IExecutionContext executionContext)
    {
        _repository = repository;
        _dataScopeService = dataScopeService;
        _executionContext = executionContext;
    }

    public async Task<Result> Handle(
        CancelLeaveRequestCommand request,
        CancellationToken cancellationToken)
    {
        var leaveRequest = await _repository.GetByIdAsync(request.LeaveRequestId, cancellationToken);
        if (leaveRequest is null)
            return Result.Failure(LeaveErrors.LeaveRequestNotFound(request.LeaveRequestId));

        var rule = await _dataScopeService.GetScopeRuleAsync(
            _executionContext.UserId, AttendancePermissions.Leave.Request, cancellationToken);

        // Only the owner or someone with approve permission can cancel
        var isOwner = rule.SelfEmployeeId.HasValue && rule.SelfEmployeeId.Value == leaveRequest.EmployeeId;
        if (!isOwner)
        {
            var approveRule = await _dataScopeService.GetScopeRuleAsync(
                _executionContext.UserId, AttendancePermissions.Leave.Approve, cancellationToken);
            if (approveRule.Level.Category == ScopeCategory.None)
                return Result.Failure(LeaveErrors.ApproveForbidden());
        }

        leaveRequest.Cancel();
        _repository.Update(leaveRequest);

        return Result.Success();
    }
}
