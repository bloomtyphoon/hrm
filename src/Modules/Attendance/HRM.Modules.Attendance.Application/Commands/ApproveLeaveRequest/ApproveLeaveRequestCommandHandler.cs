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

        var approverEmployeeId = rule.SelfEmployeeId.Value;

        // Multi-level approval flow
        if (leaveRequest.CurrentApprovalStep.HasValue)
        {
            var currentStep = await _stepRepository.GetByRequestAndStepOrderAsync(
                request.LeaveRequestId, leaveRequest.CurrentApprovalStep.Value, cancellationToken);

            if (currentStep is null)
                return Result.Failure(LeaveErrors.ApprovalStepNotFound(leaveRequest.CurrentApprovalStep.Value));

            // Validate that the current user is the designated approver for this step
            if (currentStep.ApproverEmployeeId != approverEmployeeId)
                return Result.Failure(LeaveErrors.NotCurrentApprover());

            if (request.IsApproved)
            {
                currentStep.Approve(request.Notes);
                _stepRepository.Update(currentStep);

                // Advance to next step or finalize
                leaveRequest.Approve(approverEmployeeId, request.Notes);
            }
            else
            {
                currentStep.Reject(request.Notes);
                _stepRepository.Update(currentStep);

                // Reject entire request immediately — skip remaining steps
                leaveRequest.Reject(approverEmployeeId, request.Notes);

                // Mark remaining pending steps as skipped
                var allSteps = await _stepRepository.GetByLeaveRequestIdAsync(
                    request.LeaveRequestId, cancellationToken);

                foreach (var remainingStep in allSteps)
                {
                    if (remainingStep.Status == Domain.Entities.LeaveApprovalStepStatus.Pending
                        && remainingStep.StepOrder > leaveRequest.CurrentApprovalStep.Value)
                    {
                        remainingStep.Skip("Skipped due to rejection at earlier step.");
                        _stepRepository.Update(remainingStep);
                    }
                }
            }
        }
        else
        {
            // Single-level approval (no multi-step chain)
            if (request.IsApproved)
                leaveRequest.Approve(approverEmployeeId, request.Notes);
            else
                leaveRequest.Reject(approverEmployeeId, request.Notes);
        }

        _repository.Update(leaveRequest);

        return Result.Success();
    }
}
