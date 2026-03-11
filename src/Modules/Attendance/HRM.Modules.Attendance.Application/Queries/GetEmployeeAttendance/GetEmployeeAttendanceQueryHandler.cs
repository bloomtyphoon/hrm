using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Application.Pagination;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Attendance.Application.Abstractions.Data;
using HRM.Modules.Attendance.Application.Queries.GetMyAttendance;
using HRM.Modules.Attendance.Application.Security;
using HRM.Modules.Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Attendance.Application.Queries.GetEmployeeAttendance;

public sealed class GetEmployeeAttendanceQueryHandler
    : IQueryHandler<GetEmployeeAttendanceQuery, PagedResult<AttendanceSummaryDto>>
{
    private readonly IAttendanceQueryContext _context;
    private readonly IDataScopeService _dataScopeService;
    private readonly IExecutionContext _executionContext;

    public GetEmployeeAttendanceQueryHandler(
        IAttendanceQueryContext context,
        IDataScopeService dataScopeService,
        IExecutionContext executionContext)
    {
        _context = context;
        _dataScopeService = dataScopeService;
        _executionContext = executionContext;
    }

    public async Task<PagedResult<AttendanceSummaryDto>> Handle(
        GetEmployeeAttendanceQuery request,
        CancellationToken cancellationToken)
    {
        var rule = await _dataScopeService.GetScopeRuleAsync(
            _executionContext.UserId, AttendancePermissions.Record.View, cancellationToken);

        var query = _context.AttendanceRecords
            .AsNoTracking()
            .Where(r => r.EmployeeId == request.EmployeeId);

        // Apply scope: employee must be within current user's access
        query = ApplyScopeRule(query, rule);

        if (request.FromDate.HasValue)
            query = query.Where(r => r.Date >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(r => r.Date <= request.ToDate.Value);

        query = query
            .OrderByDescending(r => r.Date)
            .ThenByDescending(r => r.CheckInTimeUtc);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(r => new AttendanceSummaryDto
            {
                Id = r.Id,
                EmployeeId = r.EmployeeId,
                Date = r.Date,
                CheckInTimeUtc = r.CheckInTimeUtc,
                CheckOutTimeUtc = r.CheckOutTimeUtc,
                Status = r.Status.ToString(),
                IsManualEntry = r.IsManualEntry
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<AttendanceSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
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
