using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Application.Pagination;
using HRM.Modules.Attendance.Application.Abstractions.Data;
using HRM.Modules.Attendance.Application.Security;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Attendance.Application.Queries.GetMyAttendance;

public sealed class GetMyAttendanceQueryHandler
    : IQueryHandler<GetMyAttendanceQuery, PagedResult<AttendanceSummaryDto>>
{
    private readonly IAttendanceQueryContext _context;
    private readonly IDataScopeService _dataScopeService;
    private readonly IExecutionContext _executionContext;

    public GetMyAttendanceQueryHandler(
        IAttendanceQueryContext context,
        IDataScopeService dataScopeService,
        IExecutionContext executionContext)
    {
        _context = context;
        _dataScopeService = dataScopeService;
        _executionContext = executionContext;
    }

    public async Task<PagedResult<AttendanceSummaryDto>> Handle(
        GetMyAttendanceQuery request,
        CancellationToken cancellationToken)
    {
        var rule = await _dataScopeService.GetScopeRuleAsync(
            _executionContext.UserId, AttendancePermissions.Record.View, cancellationToken);

        // GetMyAttendance is always scoped to the current user's own records
        if (rule.SelfEmployeeId is null)
            return PagedResult<AttendanceSummaryDto>.Empty(request.PageNumber, request.PageSize);

        var query = _context.AttendanceRecords
            .AsNoTracking()
            .Where(r => r.EmployeeId == rule.SelfEmployeeId.Value);

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
}
