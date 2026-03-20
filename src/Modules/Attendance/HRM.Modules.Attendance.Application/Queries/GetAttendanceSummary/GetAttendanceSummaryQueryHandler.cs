using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Attendance.Application.Abstractions.Data;
using HRM.Modules.Attendance.Application.Security;
using HRM.Modules.Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Attendance.Application.Queries.GetAttendanceSummary;

public sealed class GetAttendanceSummaryQueryHandler
    : IQueryHandler<GetAttendanceSummaryQuery, IReadOnlyList<AttendanceDailySummaryDto>>
{
    private readonly IAttendanceQueryContext _context;
    private readonly IDataScopeService _dataScopeService;
    private readonly IExecutionContext _executionContext;

    public GetAttendanceSummaryQueryHandler(
        IAttendanceQueryContext context,
        IDataScopeService dataScopeService,
        IExecutionContext executionContext)
    {
        _context = context;
        _dataScopeService = dataScopeService;
        _executionContext = executionContext;
    }

    public async Task<IReadOnlyList<AttendanceDailySummaryDto>> Handle(
        GetAttendanceSummaryQuery request,
        CancellationToken cancellationToken)
    {
        var rule = await _dataScopeService.GetScopeRuleAsync(
            _executionContext.UserId, AttendancePermissions.Record.View, cancellationToken);

        var query = _context.AttendanceRecords.AsNoTracking();

        query = ApplyScopeRule(query, rule);

        var fromDate = request.FromDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30));
        var toDate = request.ToDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        query = query.Where(r => r.Date >= fromDate && r.Date <= toDate);

        var summary = await query
            .GroupBy(r => r.Date)
            .Select(g => new AttendanceDailySummaryDto
            {
                Date = g.Key,
                TotalEmployees = g.Select(r => r.EmployeeId).Distinct().Count(),
                CheckedIn = g.Count(r => r.Status == AttendanceStatus.CheckedIn),
                CheckedOut = g.Count(r => r.Status == AttendanceStatus.CheckedOut),
                ManualEntries = g.Count(r => r.IsManualEntry)
            })
            .OrderByDescending(s => s.Date)
            .ToListAsync(cancellationToken);

        return summary;
    }

    private static IQueryable<AttendanceRecord> ApplyScopeRule(
        IQueryable<AttendanceRecord> query, DataScopeRule rule)
    {
        var companyIds = rule.DimensionIds.ToList();
        return rule.Level.Category switch
        {
            ScopeCategory.Global => query,
            ScopeCategory.None => query.Where(_ => false),
            ScopeCategory.Set when rule.Level == DataScopeLevel.Self =>
                query.Where(r => r.EmployeeId == rule.SelfEmployeeId!.Value),
            ScopeCategory.Set =>
                query.Where(r => rule.EmployeeIds.Contains(r.EmployeeId)),
            ScopeCategory.Dimension =>
                query.Where(r => r.CompanyId != null && companyIds.Contains(r.CompanyId.Value)),
            _ => query.Where(_ => false)
        };
    }
}
