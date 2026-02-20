using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Personnel.Application.Abstractions.Data;
using HRM.Modules.Personnel.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Personnel.Application.Queries.GetEmployeeAssignments;

/// <summary>
/// Handler for GetEmployeeAssignmentsQuery.
///
/// Access rules (resolved via IDataScopeService):
/// - Global (System): sees all assignments for the given employee.
/// - Company scope: sees only assignments in the employee's accessible companies (multi-company support).
/// - None: no results.
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
            _executionContext.UserId, "Personnel.Employee.View", cancellationToken);

        var query = _context.EmployeeAssignments
            .AsNoTracking()
            .Where(a => a.EmployeeId == request.EmployeeId);

        query = ApplyScopeRule(query, rule);

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

    private static IQueryable<EmployeeAssignment> ApplyScopeRule(
        IQueryable<EmployeeAssignment> query,
        DataScopeRule rule)
    {
        return rule.Level switch
        {
            DataScopeLevel.Global => query,
            DataScopeLevel.None => query.Where(_ => false),
            DataScopeLevel.Company => BuildCompanyFilter(query, rule.DimensionIds),
            // Self/EmployeeSet: assignment visibility matches employee ownership (EmployeeId filter above)
            DataScopeLevel.Self or DataScopeLevel.EmployeeSet => query,
            _ => query.Where(_ => false)
        };
    }

    private static IQueryable<EmployeeAssignment> BuildCompanyFilter(
        IQueryable<EmployeeAssignment> query,
        IReadOnlyCollection<Guid> companyIds)
    {
        var ids = companyIds.ToList();
        return query.Where(a => ids.Contains(a.CompanyId));
    }
}
