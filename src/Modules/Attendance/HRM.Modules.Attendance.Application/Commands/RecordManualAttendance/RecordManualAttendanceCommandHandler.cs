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

namespace HRM.Modules.Attendance.Application.Commands.RecordManualAttendance;

internal sealed class RecordManualAttendanceCommandHandler
    : ICommandHandler<RecordManualAttendanceCommand, Guid>
{
    private readonly IAttendanceRecordRepository _repository;
    private readonly ITenantContext _tenantContext;
    private readonly IDataScopeService _dataScopeService;
    private readonly IExecutionContext _executionContext;

    public RecordManualAttendanceCommandHandler(
        IAttendanceRecordRepository repository,
        ITenantContext tenantContext,
        IDataScopeService dataScopeService,
        IExecutionContext executionContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
        _dataScopeService = dataScopeService;
        _executionContext = executionContext;
    }

    public async Task<Result<Guid>> Handle(
        RecordManualAttendanceCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Verify the current user's data scope allows recording for this employee's company
        var rule = await _dataScopeService.GetScopeRuleAsync(
            _executionContext.UserId, AttendancePermissions.Record.ManualRecord, cancellationToken);

        if (!IsEmployeeInScope(rule, request))
            return Result.Failure<Guid>(AttendanceErrors.ManualRecordForbidden(request.EmployeeId));

        // 2. Enforce one-record-per-day (HR cannot overwrite an existing record)
        if (await _repository.HasRecordForDateAsync(request.EmployeeId, request.Date, cancellationToken))
            return Result.Failure<Guid>(AttendanceErrors.AlreadyCheckedIn(request.EmployeeId));

        // 3. Get tenant ID
        var tenantId = _tenantContext.TenantId
            ?? throw new InvalidOperationException("TenantId is required to record attendance.");

        // 4. Create manual attendance record (domain validates time order)
        var record = AttendanceRecord.CreateManual(
            tenantId: tenantId,
            employeeId: request.EmployeeId,
            companyId: request.CompanyId,
            date: request.Date,
            checkInTimeUtc: request.CheckInTimeUtc,
            checkOutTimeUtc: request.CheckOutTimeUtc,
            notes: request.Notes);

        _repository.Add(record);

        return Result.Success(record.Id);
    }

    // ManualRecord permission only supports Company and Global scopes (per PermissionCatalog.xml).
    // For Company scope: CompanyId must be provided and fall within the user's allowed companies.
    private static bool IsEmployeeInScope(DataScopeRule rule, RecordManualAttendanceCommand request)
        => rule.Level.Category switch
        {
            ScopeCategory.Global => true,
            ScopeCategory.Dimension => request.CompanyId.HasValue
                && rule.DimensionIds.Contains(request.CompanyId.Value),
            _ => false
        };
}
