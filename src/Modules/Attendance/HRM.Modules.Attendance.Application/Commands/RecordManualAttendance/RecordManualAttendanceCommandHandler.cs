using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Application.Abstractions.Multitenancy;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Attendance.Application.Abstractions;
using HRM.Modules.Attendance.Domain.Entities;
using HRM.Modules.Attendance.Domain.Errors;

namespace HRM.Modules.Attendance.Application.Commands.RecordManualAttendance;

internal sealed class RecordManualAttendanceCommandHandler
    : ICommandHandler<RecordManualAttendanceCommand, Guid>
{
    private readonly IAttendanceRecordRepository _repository;
    private readonly ITenantContext _tenantContext;

    public RecordManualAttendanceCommandHandler(
        IAttendanceRecordRepository repository,
        ITenantContext tenantContext)
    {
        _repository = repository;
        _tenantContext = tenantContext;
    }

    public async Task<Result<Guid>> Handle(
        RecordManualAttendanceCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Enforce one-record-per-day (HR cannot overwrite an existing record)
        if (await _repository.HasRecordForDateAsync(request.EmployeeId, request.Date, cancellationToken))
            return Result.Failure<Guid>(AttendanceErrors.AlreadyCheckedIn(request.EmployeeId));

        // 2. Get tenant ID
        var tenantId = _tenantContext.TenantId
            ?? throw new InvalidOperationException("TenantId is required to record attendance.");

        // 3. Create manual attendance record (domain validates time order)
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
}
