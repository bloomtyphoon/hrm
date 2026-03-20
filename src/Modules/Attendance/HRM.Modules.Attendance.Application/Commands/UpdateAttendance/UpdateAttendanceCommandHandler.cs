using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Attendance.Application.Abstractions;
using HRM.Modules.Attendance.Application.Security;
using HRM.Modules.Attendance.Domain.Errors;

namespace HRM.Modules.Attendance.Application.Commands.UpdateAttendance;

internal sealed class UpdateAttendanceCommandHandler
    : ICommandHandler<UpdateAttendanceCommand>
{
    private readonly IAttendanceRecordRepository _repository;
    private readonly IDataScopeService _dataScopeService;
    private readonly IExecutionContext _executionContext;

    public UpdateAttendanceCommandHandler(
        IAttendanceRecordRepository repository,
        IDataScopeService dataScopeService,
        IExecutionContext executionContext)
    {
        _repository = repository;
        _dataScopeService = dataScopeService;
        _executionContext = executionContext;
    }

    public async Task<Result> Handle(
        UpdateAttendanceCommand request,
        CancellationToken cancellationToken)
    {
        var record = await _repository.GetByIdAsync(request.RecordId, cancellationToken);
        if (record is null)
            return Result.Failure(AttendanceErrors.RecordNotFound(request.RecordId));

        var rule = await _dataScopeService.GetScopeRuleAsync(
            _executionContext.UserId, AttendancePermissions.Record.Update, cancellationToken);

        if (!IsRecordInScope(rule, record))
            return Result.Failure(AttendanceErrors.UpdateForbidden(request.RecordId));

        record.Update(request.CheckInTimeUtc, request.CheckOutTimeUtc, request.Notes);
        _repository.Update(record);

        return Result.Success();
    }

    private static bool IsRecordInScope(DataScopeRule rule, Domain.Entities.AttendanceRecord record)
        => rule.Level.Category switch
        {
            ScopeCategory.Global => true,
            ScopeCategory.Dimension => record.CompanyId.HasValue
                && rule.DimensionIds.Contains(record.CompanyId.Value),
            _ => false
        };
}
