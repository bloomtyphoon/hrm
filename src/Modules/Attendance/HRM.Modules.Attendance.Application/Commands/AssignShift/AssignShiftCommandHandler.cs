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

namespace HRM.Modules.Attendance.Application.Commands.AssignShift;

internal sealed class AssignShiftCommandHandler
    : ICommandHandler<AssignShiftCommand, Guid>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IShiftAssignmentRepository _assignmentRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IDataScopeService _dataScopeService;
    private readonly IExecutionContext _executionContext;

    public AssignShiftCommandHandler(
        IShiftRepository shiftRepository,
        IShiftAssignmentRepository assignmentRepository,
        ITenantContext tenantContext,
        IDataScopeService dataScopeService,
        IExecutionContext executionContext)
    {
        _shiftRepository = shiftRepository;
        _assignmentRepository = assignmentRepository;
        _tenantContext = tenantContext;
        _dataScopeService = dataScopeService;
        _executionContext = executionContext;
    }

    public async Task<Result<Guid>> Handle(
        AssignShiftCommand request,
        CancellationToken cancellationToken)
    {
        var rule = await _dataScopeService.GetScopeRuleAsync(
            _executionContext.UserId, AttendancePermissions.Shift.Assign, cancellationToken);

        if (rule.Level.Category == ScopeCategory.None)
            return Result.Failure<Guid>(ShiftErrors.AssignForbidden());

        var shift = await _shiftRepository.GetByIdAsync(request.ShiftId, cancellationToken);
        if (shift is null)
            return Result.Failure<Guid>(ShiftErrors.ShiftNotFound(request.ShiftId));

        var tenantId = _tenantContext.TenantId
            ?? throw new InvalidOperationException("TenantId is required.");

        var assignment = ShiftAssignment.Create(
            tenantId: tenantId,
            shiftId: request.ShiftId,
            employeeId: request.EmployeeId,
            effectiveFrom: request.EffectiveFrom,
            effectiveTo: request.EffectiveTo,
            companyId: request.CompanyId);

        _assignmentRepository.Add(assignment);

        return Result.Success(assignment.Id);
    }
}
