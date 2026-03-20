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

        if (!string.IsNullOrWhiteSpace(request.Status))
            query = query.Where(x => x.Request.Status.ToString() == request.Status);

        query = query.OrderByDescending(x => x.Request.StartDate);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new LeaveRequestDto
            {
                Id = x.Request.Id,
                EmployeeId = x.Request.EmployeeId,
                LeaveTypeId = x.Request.LeaveTypeId,
                LeaveTypeName = x.TypeName,
                StartDate = x.Request.StartDate,
                EndDate = x.Request.EndDate,
                TotalDays = x.Request.TotalDays,
                Reason = x.Request.Reason,
                Status = x.Request.Status.ToString(),
                ApprovedByEmployeeId = x.Request.ApprovedByEmployeeId,
                DecisionDateUtc = x.Request.DecisionDateUtc,
                DecisionNotes = x.Request.DecisionNotes
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<LeaveRequestDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
