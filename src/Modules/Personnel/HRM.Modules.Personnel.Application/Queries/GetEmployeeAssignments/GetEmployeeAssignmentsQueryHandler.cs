using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Personnel.Application.Abstractions.Data;
using HRM.Modules.Personnel.Application.Security;
using HRM.Modules.Personnel.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Personnel.Application.Queries.GetEmployeeAssignments;

/// <summary>
/// Handler for GetEmployeeAssignmentsQuery.
///
/// Two-layer access control:
/// 1. Employee-level: Can the user access this employee at all? (via EmployeeScopeFilter)
/// 2. Assignment-level: For dimension scopes, further filter which assignments are visible.
///    - Company scope: only assignments in accessible companies.
///    - Department/Position scope: only assignments in accessible departments/positions.
/// </summary>
public sealed class GetEmployeeAssignmentsQueryHandler
    : IQueryHandler<GetEmployeeAssignmentsQuery, List<AssignmentDto>>
{
    private readonly IPersonnelQueryContext _context;
    private readonly IDataScopeService _dataScopeService;
    private readonly IExecutionContext _executionContext;

    public GetEmployeeAssignmentsQueryHandler(
        IPersonnelQueryContext context,
        IDataScopeService dataScopeService,
        IExecutionContext executionContext)
    {
        _context = context;
        _dataScopeService = dataScopeService;
        _executionContext = executionContext;
    }

    public async Task<List<AssignmentDto>> Handle(
        GetEmployeeAssignmentsQuery request,
        CancellationToken cancellationToken)
    {
        var rule = await _dataScopeService.GetScopeRuleAsync(
            _executionContext.UserId, PersonnelPermissions.Employee.View, cancellationToken);

        // Layer 1: Check if the user can access this employee at all
        if (!await EmployeeScopeFilter.IsAccessibleAsync(rule, request.EmployeeId, _context, cancellationToken))
        {
            return [];
        }

        var query = _context.EmployeeAssignments
            .AsNoTracking()
            .Where(a => a.EmployeeId == request.EmployeeId);

        // Layer 2: For dimension scopes, further filter which assignments are visible
        if (rule.Level.Category == ScopeCategory.Dimension)
        {
            var ids = rule.DimensionIds.ToList();
            query = rule.Level.DimensionKey switch
            {
                DimensionKeys.Company => query.Where(a => ids.Contains(a.CompanyId)),
                DimensionKeys.Department => query.Where(a => ids.Contains(a.DepartmentId)),
                DimensionKeys.Position => query.Where(a => ids.Contains(a.PositionId)),
                _ => query
            };
        }

        if (request.Status.HasValue)
        {
            query = query.Where(a => a.Status == request.Status.Value);
        }

        return await query
            .OrderByDescending(a => a.IsPrimary)
            .ThenByDescending(a => a.StartDate)
            .Select(a => new AssignmentDto
            {
                Id = a.Id,
                EmployeeId = a.EmployeeId,
                CompanyId = a.CompanyId,
                DepartmentId = a.DepartmentId,
                PositionId = a.PositionId,
                StartDate = a.StartDate,
                EndDate = a.EndDate,
                IsPrimary = a.IsPrimary,
                Status = a.Status
            })
            .ToListAsync(cancellationToken);
    }
}
