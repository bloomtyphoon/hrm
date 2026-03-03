using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Attendance.Application.Abstractions.Data;
using HRM.Modules.Attendance.Application.Security;
using HRM.Modules.Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Attendance.Application.Queries.GetAttendanceById;

public sealed class GetAttendanceByIdQueryHandler
    : IQueryHandler<GetAttendanceByIdQuery, AttendanceRecordDetailDto?>
{
    private readonly IAttendanceQueryContext _context;
    private readonly IDataScopeService _dataScopeService;
    private readonly IExecutionContext _executionContext;

    public GetAttendanceByIdQueryHandler(
        IAttendanceQueryContext context,
        IDataScopeService dataScopeService,
        IExecutionContext executionContext)
    {
        _context = context;
        _dataScopeService = dataScopeService;
        _executionContext = executionContext;
    }

    public async Task<AttendanceRecordDetailDto?> Handle(
        GetAttendanceByIdQuery request,
        CancellationToken cancellationToken)
    {
        var rule = await _dataScopeService.GetScopeRuleAsync(
            _executionContext.UserId, AttendancePermissions.Record.View, cancellationToken);

        var query = _context.AttendanceRecords
            .AsNoTracking()
            .Where(r => r.Id == request.RecordId);

        query = ApplyScopeRule(query, rule);

        return await query
            .Select(r => new AttendanceRecordDetailDto
            {
                Id = r.Id,
                EmployeeId = r.EmployeeId,
                Date = r.Date,
                CheckInTimeUtc = r.CheckInTimeUtc,
                CheckOutTimeUtc = r.CheckOutTimeUtc,
                Status = r.Status.ToString(),
                IsManualEntry = r.IsManualEntry,
                Notes = r.Notes,
                CompanyId = r.CompanyId,
                CreatedAtUtc = r.CreatedAtUtc,
                ModifiedAtUtc = r.ModifiedAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static IQueryable<AttendanceRecord> ApplyScopeRule(
        IQueryable<AttendanceRecord> query, DataScopeRule rule)
    {
        var companyIds = rule.DimensionIds.ToList();
        return rule.Level switch
        {
            DataScopeLevel.Global      => query,
            DataScopeLevel.None        => query.Where(_ => false),
            DataScopeLevel.Self        => query.Where(r => r.EmployeeId == rule.SelfEmployeeId!.Value),
            DataScopeLevel.EmployeeSet => query.Where(r => rule.EmployeeIds.Contains(r.EmployeeId)),
            DataScopeLevel.Company     => query.Where(r => r.CompanyId != null && companyIds.Contains(r.CompanyId.Value)),
            _ => query.Where(_ => false)
        };
    }
}
