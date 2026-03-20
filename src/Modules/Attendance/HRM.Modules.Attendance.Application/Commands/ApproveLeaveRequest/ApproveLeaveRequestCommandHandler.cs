using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Attendance.Application.Abstractions;
using HRM.Modules.Attendance.Application.Security;
using HRM.Modules.Attendance.Domain.Errors;

namespace HRM.Modules.Attendance.Application.Commands.ApproveLeaveRequest;

internal sealed class ApproveLeaveRequestCommandHandler
    : ICommandHandler<ApproveLeaveRequestCommand>
{
    private readonly ILeaveRequestRepository _repository;
    private readonly ILeaveApprovalStepRepository _stepRepository;
    private readonly IDataScopeService _dataScopeService;
    private readonly IExecutionContext _executionContext;

    public ApproveLeaveRequestCommandHandler(
        ILeaveRequestRepository repository,
        ILeaveApprovalStepRepository stepRepository,
        IDataScopeService dataScopeService,
        IExecutionContext executionContext)
    {
        _repository = repository;
        _stepRepository = stepRepository;
        _dataScopeService = dataScopeService;
        _executionContext = executionContext;
    }

    public async Task<Result> Handle(
        ApproveLeaveRequestCommand request,
        CancellationToken cancellationToken)
    {
        var leaveRequest = await _repository.GetByIdAsync(request.LeaveRequestId, cancellationToken);
        if (leaveRequest is null)
            return Result.Failure(LeaveErrors.LeaveRequestNotFound(request.LeaveRequestId));

        var rule = await _dataScopeService.GetScopeRuleAsync(
            _executionContext.UserId, AttendancePermissions.Leave.Approve, cancellationToken);

        if (rule.Level.Category == ScopeCategory.None)
            return Result.Failure(LeaveErrors.ApproveForbidden());

        if (rule.SelfEmployeeId is null)
            return Result.Failure(LeaveErrors.EmployeeNotResolved());

        // Track approval step if multi-level
        if (leaveRequest.CurrentApprovalStep.HasValue)
        {
            var step = await _stepRepository.GetByRequestAndStepOrderAsync(
                request.LeaveRequestId, leaveRequest.CurrentApprovalStep.Value, cancellationToken);

            if (step is not null)
            {
                if (request.IsApproved)
                    step.Approve(request.Notes);
                else
                    step.Reject(request.Notes);

                _stepRepository.Update(step);
            }
        }

        if (request.IsApproved)
            leaveRequest.Approve(rule.SelfEmployeeId.Value, request.Notes);
        else
            leaveRequest.Reject(rule.SelfEmployeeId.Value, request.Notes);

        _repository.Update(leaveRequest);

        return Result.Success();
    }
}
