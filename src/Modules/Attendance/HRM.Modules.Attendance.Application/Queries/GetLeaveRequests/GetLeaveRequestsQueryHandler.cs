using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Application.Pagination;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Attendance.Application.Abstractions.Data;
using HRM.Modules.Attendance.Application.Security;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Attendance.Application.Queries.GetLeaveRequests;

public sealed class GetLeaveRequestsQueryHandler
    : IQueryHandler<GetLeaveRequestsQuery, PagedResult<LeaveRequestDto>>
{
    private readonly IAttendanceQueryContext _context;
    private readonly IDataScopeService _dataScopeService;
    private readonly IExecutionContext _executionContext;

    public GetLeaveRequestsQueryHandler(
        IAttendanceQueryContext context,
        IDataScopeService dataScopeService,
        IExecutionContext executionContext)
    {
        _context = context;
        _dataScopeService = dataScopeService;
        _executionContext = executionContext;
    }

    public async Task<PagedResult<LeaveRequestDto>> Handle(
        GetLeaveRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var rule = await _dataScopeService.GetScopeRuleAsync(
            _executionContext.UserId, AttendancePermissions.Leave.ViewRequests, cancellationToken);

        var query = from lr in _context.LeaveRequests.AsNoTracking()
                    join lt in _context.LeaveTypes.AsNoTracking() on lr.LeaveTypeId equals lt.Id
                    select new { Request = lr, TypeName = lt.Name };

        // Apply scope
        var companyIds = rule.DimensionIds.ToList();
        query = rule.Level.Category switch
        {
            ScopeCategory.Global => query,
            ScopeCategory.Set when rule.Level == DataScopeLevel.Self =>
                query.Where(x => x.Request.EmployeeId == rule.SelfEmployeeId!.Value),
            ScopeCategory.Set =>
                query.Where(x => rule.EmployeeIds.Contains(x.Request.EmployeeId)),
            ScopeCategory.Dimension =>
                query.Where(x => x.Request.CompanyId != null && companyIds.Contains(x.Request.CompanyId.Value)),
            _ => query.Where(_ => false)
        };

        if (request.EmployeeId.HasValue)
            query = query.Where(x => x.Request.EmployeeId == request.EmployeeId.Value);

        if (!string.IsNullOrWhiteSpace(request.Status)
            && Enum.TryParse<Domain.Entities.LeaveStatus>(request.Status, ignoreCase: true, out var statusFilter))
            query = query.Where(x => x.Request.Status == statusFilter);

        query = query.OrderByDescending(x => x.Request.StartDate);

        var totalCount = await query.CountAsync(cancellationToken);

        var rows = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new
            {
                x.Request.Id,
                x.Request.EmployeeId,
                x.Request.LeaveTypeId,
                x.TypeName,
                x.Request.StartDate,
                x.Request.EndDate,
                x.Request.Reason,
                x.Request.Status,
                x.Request.ApprovedByEmployeeId,
                x.Request.DecisionDateUtc,
                x.Request.DecisionNotes
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(x => new LeaveRequestDto
        {
            Id = x.Id,
            EmployeeId = x.EmployeeId,
            LeaveTypeId = x.LeaveTypeId,
            LeaveTypeName = x.TypeName,
            StartDate = x.StartDate,
            EndDate = x.EndDate,
            TotalDays = x.EndDate.DayNumber - x.StartDate.DayNumber + 1,
            Reason = x.Reason,
            Status = x.Status.ToString(),
            ApprovedByEmployeeId = x.ApprovedByEmployeeId,
            DecisionDateUtc = x.DecisionDateUtc,
            DecisionNotes = x.DecisionNotes
        }).ToList();

        return new PagedResult<LeaveRequestDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
