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

    public GetEmployeeAssignmentsQueryHandler(IPersonnelQueryContext context)
    {
        _context = context;
    }

    public async Task<List<AssignmentDto>> Handle(
        GetEmployeeAssignmentsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.EmployeeAssignments
            .AsNoTracking()
            .Where(a => a.EmployeeId == request.EmployeeId);

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
