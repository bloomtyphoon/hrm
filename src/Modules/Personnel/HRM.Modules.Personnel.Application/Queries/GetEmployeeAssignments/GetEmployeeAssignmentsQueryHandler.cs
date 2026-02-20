using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.Modules.Personnel.Application.Abstractions.Data;
using HRM.Modules.Personnel.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Personnel.Application.Queries.GetEmployeeAssignments;

/// <summary>
/// Handler for GetEmployeeAssignmentsQuery.
///
/// Access rules:
/// - System: sees all assignments for the given employee.
/// - Employee: sees ONLY assignments belonging to their own company (from JWT CompanyId claim).
///   No valid claim → no results (query.Where(_ => false)).
/// </summary>
public sealed class GetEmployeeAssignmentsQueryHandler
    : IQueryHandler<GetEmployeeAssignmentsQuery, List<AssignmentDto>>
{
    private readonly IPersonnelQueryContext _context;
    private readonly IExecutionContext _executionContext;

    public GetEmployeeAssignmentsQueryHandler(
        IPersonnelQueryContext context,
        IExecutionContext executionContext)
    {
        _context = context;
        _executionContext = executionContext;
    }

    public async Task<List<AssignmentDto>> Handle(
        GetEmployeeAssignmentsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.EmployeeAssignments
            .AsNoTracking()
            .Where(a => a.EmployeeId == request.EmployeeId);

        // Company scope — Employee accounts restricted to their own company's assignments
        if (IsEmployeeAccount())
            query = ApplyEmployeeCompanyScope(query);

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

    /// <summary>
    /// Employee account: ONLY sees assignments belonging to their own company.
    /// No valid CompanyId claim → no results (query.Where(_ => false)).
    /// </summary>
    private IQueryable<EmployeeAssignment> ApplyEmployeeCompanyScope(IQueryable<EmployeeAssignment> query)
    {
        var companyIdClaim = _executionContext.GetClaimValue("CompanyId");
        if (!Guid.TryParse(companyIdClaim, out var companyId))
            return query.Where(_ => false);

        return query.Where(a => a.CompanyId == companyId);
    }

    private bool IsEmployeeAccount() =>
        _executionContext.GetClaimValue("AccountType") == "Employee";
}
