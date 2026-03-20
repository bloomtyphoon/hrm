using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Application.Abstractions.Multitenancy;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Attendance.Application.Abstractions;
using HRM.Modules.Attendance.Application.Security;
using HRM.Modules.Attendance.Domain.Entities;
using HRM.Modules.Attendance.Domain.Errors;

namespace HRM.Modules.Attendance.Application.Commands.SubmitLeaveRequest;

internal sealed class SubmitLeaveRequestCommandHandler
    : ICommandHandler<SubmitLeaveRequestCommand, Guid>
{
    private readonly ILeaveRequestRepository _leaveRequestRepository;
    private readonly ILeaveTypeRepository _leaveTypeRepository;
    private readonly ILeaveApprovalSettingRepository _approvalSettingRepository;
    private readonly ILeaveApprovalStepRepository _stepRepository;
    private readonly IApprovalChainResolver _approvalChainResolver;
    private readonly ITenantContext _tenantContext;
    private readonly IDataScopeService _dataScopeService;
    private readonly IExecutionContext _executionContext;

    public SubmitLeaveRequestCommandHandler(
        ILeaveRequestRepository leaveRequestRepository,
        ILeaveTypeRepository leaveTypeRepository,
        ILeaveApprovalSettingRepository approvalSettingRepository,
        ILeaveApprovalStepRepository stepRepository,
        IApprovalChainResolver approvalChainResolver,
        ITenantContext tenantContext,
        IDataScopeService dataScopeService,
        IExecutionContext executionContext)
    {
        _leaveRequestRepository = leaveRequestRepository;
        _leaveTypeRepository = leaveTypeRepository;
        _approvalSettingRepository = approvalSettingRepository;
        _stepRepository = stepRepository;
        _approvalChainResolver = approvalChainResolver;
        _tenantContext = tenantContext;
        _dataScopeService = dataScopeService;
        _executionContext = executionContext;
    }

    public async Task<Result<Guid>> Handle(
        SubmitLeaveRequestCommand request,
        CancellationToken cancellationToken)
    {
        var rule = await _dataScopeService.GetScopeRuleAsync(
            _executionContext.UserId, AttendancePermissions.Leave.Request, cancellationToken);

        if (rule.SelfEmployeeId is null)
            return Result.Failure<Guid>(LeaveErrors.EmployeeNotResolved());

        var leaveType = await _leaveTypeRepository.GetByIdAsync(request.LeaveTypeId, cancellationToken);
        if (leaveType is null)
            return Result.Failure<Guid>(LeaveErrors.LeaveTypeNotFound(request.LeaveTypeId));

        var tenantId = _tenantContext.TenantId
            ?? throw new InvalidOperationException("TenantId is required.");

        var employeeId = rule.SelfEmployeeId.Value;

        var leaveRequest = LeaveRequest.Create(
            tenantId: tenantId,
            employeeId: employeeId,
            leaveTypeId: request.LeaveTypeId,
            startDate: request.StartDate,
            endDate: request.EndDate,
            reason: request.Reason);

        // Apply approval settings
        var settings = await _approvalSettingRepository.GetByTenantIdAsync(tenantId, cancellationToken);

        if (settings is not null && !settings.RequiresApproval)
        {
            leaveRequest.AutoApprove();
        }
        else if (settings is not null
            && settings.AutoApproveIfDaysLessThanOrEqual.HasValue
            && leaveRequest.TotalDays <= settings.AutoApproveIfDaysLessThanOrEqual.Value)
        {
            leaveRequest.AutoApprove();
        }
        else
        {
            // Resolve multi-level approval chain (Manager → DepartmentHead → CompanyLevel)
            var maxLevels = settings?.MaxApprovalLevels ?? 1;
            var chain = await _approvalChainResolver.ResolveAsync(employeeId, maxLevels, cancellationToken);

            if (chain.Count == 0)
                return Result.Failure<Guid>(LeaveErrors.NoApprovalChainResolved());

            // Initialize the approval chain on the leave request
            leaveRequest.InitializeApprovalChain(chain.Count);

            // Create approval step records for each level
            foreach (var entry in chain)
            {
                var step = LeaveApprovalStep.Create(
                    tenantId: tenantId,
                    leaveRequestId: leaveRequest.Id,
                    stepOrder: entry.StepOrder,
                    approverEmployeeId: entry.ApproverEmployeeId,
                    approvalLevelName: entry.LevelName);

                _stepRepository.Add(step);
            }
        }

        _leaveRequestRepository.Add(leaveRequest);

        return Result.Success(leaveRequest.Id);
    }
}
