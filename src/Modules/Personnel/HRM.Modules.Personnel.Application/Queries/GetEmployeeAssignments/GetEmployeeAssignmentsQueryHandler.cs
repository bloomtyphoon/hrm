using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.Modules.Personnel.Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Personnel.Application.Queries.GetEmployeeAssignments;

/// <summary>
/// Handler for GetEmployeeAssignmentsQuery.
/// Returns all assignments for an employee, with optional status filtering.
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

        // Employee accounts can only see assignments that belong to their own company
        var accountType = _executionContext.GetClaimValue("AccountType");
        if (accountType == "Employee")
        {
            var companyIdClaim = _executionContext.GetClaimValue("CompanyId");
            if (!Guid.TryParse(companyIdClaim, out var employeeCompanyId))
                return new List<AssignmentDto>();

            query = query.Where(a => a.CompanyId == employeeCompanyId);
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
