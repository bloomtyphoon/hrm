using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Attendance.Application.Abstractions;
using HRM.Modules.Attendance.Application.Security;
using HRM.Modules.Attendance.Domain.Errors;

namespace HRM.Modules.Attendance.Application.Commands.CheckOut;

internal sealed class CheckOutCommandHandler : ICommandHandler<CheckOutCommand, Guid>
{
    private readonly IAttendanceRecordRepository _repository;
    private readonly IDataScopeService _dataScopeService;
    private readonly IExecutionContext _executionContext;

    public CheckOutCommandHandler(
        IAttendanceRecordRepository repository,
        IDataScopeService dataScopeService,
        IExecutionContext executionContext)
    {
        _repository = repository;
        _dataScopeService = dataScopeService;
        _executionContext = executionContext;
    }

    public async Task<Result<Guid>> Handle(CheckOutCommand request, CancellationToken cancellationToken)
    {
        // 1. Resolve current user's employee ID
        var rule = await _dataScopeService.GetScopeRuleAsync(
            _executionContext.UserId, AttendancePermissions.Record.CheckOut, cancellationToken);

        if (rule.SelfEmployeeId is null)
            return Result.Failure<Guid>(AttendanceErrors.EmployeeNotResolved());

        var employeeId = rule.SelfEmployeeId.Value;

        // 2. Find today's active check-in
        var checkOutTime = request.CheckOutTimeUtc ?? DateTime.UtcNow;
        var date = DateOnly.FromDateTime(checkOutTime);

        var record = await _repository.GetActiveCheckInAsync(employeeId, date, cancellationToken);
        if (record is null)
            return Result.Failure<Guid>(AttendanceErrors.ActiveCheckInNotFound(employeeId));

        // 3. Check out (domain validates time order)
        record.CheckOut(checkOutTime, request.Notes);
        _repository.Update(record);

        return Result.Success(record.Id);
    }
}
