using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Attendance.Application.Abstractions;
using HRM.Modules.Attendance.Application.Security;
using HRM.Modules.Attendance.Domain.Errors;

namespace HRM.Modules.Attendance.Application.Commands.DeleteShift;

internal sealed class DeleteShiftCommandHandler
    : ICommandHandler<DeleteShiftCommand>
{
    private readonly IShiftRepository _repository;
    private readonly IDataScopeService _dataScopeService;
    private readonly IExecutionContext _executionContext;

    public DeleteShiftCommandHandler(
        IShiftRepository repository,
        IDataScopeService dataScopeService,
        IExecutionContext executionContext)
    {
        _repository = repository;
        _dataScopeService = dataScopeService;
        _executionContext = executionContext;
    }

    public async Task<Result> Handle(
        DeleteShiftCommand request,
        CancellationToken cancellationToken)
    {
        var shift = await _repository.GetByIdAsync(request.ShiftId, cancellationToken);
        if (shift is null)
            return Result.Failure(ShiftErrors.ShiftNotFound(request.ShiftId));

        var rule = await _dataScopeService.GetScopeRuleAsync(
            _executionContext.UserId, AttendancePermissions.Shift.Manage, cancellationToken);

        if (rule.Level.Category == ScopeCategory.None)
            return Result.Failure(ShiftErrors.ManageForbidden());

        _repository.Remove(shift);

        return Result.Success();
    }
}
