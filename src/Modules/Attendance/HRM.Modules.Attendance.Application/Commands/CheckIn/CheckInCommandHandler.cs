using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Application.Abstractions.Multitenancy;
using HRM.BuildingBlocks.Application.Abstractions.Personnel;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Attendance.Application.Abstractions;
using HRM.Modules.Attendance.Application.Security;
using HRM.Modules.Attendance.Domain.Entities;
using HRM.Modules.Attendance.Domain.Errors;

namespace HRM.Modules.Attendance.Application.Commands.CheckIn;

internal sealed class CheckInCommandHandler : ICommandHandler<CheckInCommand, Guid>
{
    private readonly IAttendanceRecordRepository _repository;
    private readonly IDataScopeService _dataScopeService;
    private readonly IExecutionContext _executionContext;
    private readonly ITenantContext _tenantContext;
    private readonly IPersonnelQuery _personnelQuery;

    public CheckInCommandHandler(
        IAttendanceRecordRepository repository,
        IDataScopeService dataScopeService,
        IExecutionContext executionContext,
        ITenantContext tenantContext,
        IPersonnelQuery personnelQuery)
    {
        _repository = repository;
        _dataScopeService = dataScopeService;
        _executionContext = executionContext;
        _tenantContext = tenantContext;
        _personnelQuery = personnelQuery;
    }

    public async Task<Result<Guid>> Handle(CheckInCommand request, CancellationToken cancellationToken)
    {
        // 1. Resolve current user's employee ID from scope rule
        var rule = await _dataScopeService.GetScopeRuleAsync(
            _executionContext.UserId, AttendancePermissions.Record.CheckIn, cancellationToken);

        if (rule.SelfEmployeeId is null)
            return Result.Failure<Guid>(AttendanceErrors.EmployeeNotResolved());

        var employeeId = rule.SelfEmployeeId.Value;

        // 2. Determine check-in time and date
        var checkInTime = request.CheckInTimeUtc ?? DateTime.UtcNow;
        var date = DateOnly.FromDateTime(checkInTime);

        // 3. Enforce one-record-per-day (application-level check for meaningful error message)
        if (await _repository.HasRecordForDateAsync(employeeId, date, cancellationToken))
            return Result.Failure<Guid>(AttendanceErrors.AlreadyCheckedIn(employeeId));

        // 4. Get employee's primary company for scope-based filtering on reads
        var companyIds = await _personnelQuery.GetEmployeeCompanyIdsAsync(employeeId, cancellationToken);
        var companyId = companyIds.Count > 0 ? companyIds[0] : (Guid?)null;

        // 5. Get tenant ID
        var tenantId = _tenantContext.TenantId
            ?? throw new InvalidOperationException("TenantId is required to create an attendance record.");

        // 6. Create attendance record (raises AttendanceCheckedInDomainEvent)
        var record = AttendanceRecord.Create(tenantId, employeeId, companyId, checkInTime, request.Notes);
        _repository.Add(record);

        return Result.Success(record.Id);
    }
}
