using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Application.Pagination;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Attendance.Application.Abstractions.Data;
using HRM.Modules.Attendance.Application.Security;
using HRM.Modules.Attendance.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Attendance.Application.Queries.GetShiftAssignments;

public sealed class GetShiftAssignmentsQueryHandler
    : IQueryHandler<GetShiftAssignmentsQuery, PagedResult<ShiftAssignmentDto>>
{
    private readonly IAttendanceQueryContext _context;
    private readonly IDataScopeService _dataScopeService;
    private readonly IExecutionContext _executionContext;

    public GetShiftAssignmentsQueryHandler(
        IAttendanceQueryContext context,
        IDataScopeService dataScopeService,
        IExecutionContext executionContext)
    {
        _context = context;
        _dataScopeService = dataScopeService;
        _executionContext = executionContext;
    }

    public async Task<PagedResult<ShiftAssignmentDto>> Handle(
        GetShiftAssignmentsQuery request,
        CancellationToken cancellationToken)
    {
        var rule = await _dataScopeService.GetScopeRuleAsync(
            _executionContext.UserId, AttendancePermissions.Shift.View, cancellationToken);

        var query = from sa in _context.ShiftAssignments.AsNoTracking()
                    join s in _context.Shifts.AsNoTracking() on sa.ShiftId equals s.Id
                    select new { Assignment = sa, Shift = s };

        // Apply scope
        var companyIds = rule.DimensionIds.ToList();
        query = rule.Level.Category switch
        {
            ScopeCategory.Global => query,
            ScopeCategory.Set when rule.Level == DataScopeLevel.Self =>
                query.Where(x => x.Assignment.EmployeeId == rule.SelfEmployeeId!.Value),
            ScopeCategory.Set =>
                query.Where(x => rule.EmployeeIds.Contains(x.Assignment.EmployeeId)),
            ScopeCategory.Dimension =>
                query.Where(x => x.Assignment.CompanyId != null && companyIds.Contains(x.Assignment.CompanyId.Value)),
            _ => query.Where(_ => false)
        };

        if (request.EmployeeId.HasValue)
            query = query.Where(x => x.Assignment.EmployeeId == request.EmployeeId.Value);

        if (request.ShiftId.HasValue)
            query = query.Where(x => x.Assignment.ShiftId == request.ShiftId.Value);

        query = query.OrderByDescending(x => x.Assignment.EffectiveFrom);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new ShiftAssignmentDto
            {
                Id = x.Assignment.Id,
                ShiftId = x.Assignment.ShiftId,
                ShiftName = x.Shift.Name,
                EmployeeId = x.Assignment.EmployeeId,
                EffectiveFrom = x.Assignment.EffectiveFrom,
                EffectiveTo = x.Assignment.EffectiveTo
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<ShiftAssignmentDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
