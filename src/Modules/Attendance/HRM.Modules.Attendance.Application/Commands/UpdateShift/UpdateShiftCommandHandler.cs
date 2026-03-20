using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Application.Abstractions.Multitenancy;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Attendance.Application.Abstractions;
using HRM.Modules.Attendance.Application.Security;
using HRM.Modules.Attendance.Domain.Errors;

namespace HRM.Modules.Attendance.Application.Commands.UpdateShift;

internal sealed class UpdateShiftCommandHandler
    : ICommandHandler<UpdateShiftCommand>
{
    private readonly IShiftRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly IDataScopeService _dataScopeService;
    private readonly IExecutionContext _executionContext;

    public UpdateShiftCommandHandler(
        IShiftRepository repository,
        ITenantContext tenantContext,
        IDataScopeService dataScopeService,
        IExecutionContext executionContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
        _dataScopeService = dataScopeService;
        _executionContext = executionContext;
    }

    public async Task<Result> Handle(
        UpdateShiftCommand request,
        CancellationToken cancellationToken)
    {
        var shift = await _repository.GetByIdAsync(request.ShiftId, cancellationToken);
        if (shift is null)
            return Result.Failure(ShiftErrors.ShiftNotFound(request.ShiftId));

        var rule = await _dataScopeService.GetScopeRuleAsync(
            _executionContext.UserId, AttendancePermissions.Shift.Manage, cancellationToken);

        if (rule.Level.Category == ScopeCategory.None)
            return Result.Failure(ShiftErrors.ManageForbidden());

        var tenantId = _tenantContext.TenantId
            ?? throw new InvalidOperationException("TenantId is required.");

        if (await _repository.ExistsByNameAsync(request.Name, tenantId, request.ShiftId, cancellationToken))
            return Result.Failure(ShiftErrors.DuplicateName(request.Name));

        shift.Update(request.Name, request.StartTime, request.EndTime, request.Description);
        _repository.Update(shift);

        return Result.Success();
    }
}
