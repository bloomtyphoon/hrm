using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Application.Pagination;
using HRM.Modules.Personnel.Application.Abstractions.Data;
using HRM.Modules.Personnel.Application.Queries.GetEmployees;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Personnel.Application.Queries.GetDirectReports;

/// <summary>
/// Handler for GetDirectReportsQuery.
/// Returns paginated list of direct reports for a given manager.
/// </summary>
public sealed class GetDirectReportsQueryHandler
    : IQueryHandler<GetDirectReportsQuery, PagedResult<EmployeeSummaryDto>>
{
    private readonly IPersonnelQueryContext _context;

    public GetDirectReportsQueryHandler(IPersonnelQueryContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<EmployeeSummaryDto>> Handle(
        GetDirectReportsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Employees
            .AsNoTracking()
            .Where(e => e.ManagerId == request.ManagerId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e => new EmployeeSummaryDto
            {
                Id = e.Id,
                EmployeeCode = e.EmployeeCode,
                FirstName = e.FirstName,
                LastName = e.LastName,
                FullName = e.FirstName + " " + e.LastName,
                Email = e.Email,
                Phone = e.Phone,
                HireDate = e.HireDate,
                Status = e.Status,
                ManagerId = e.ManagerId,
                PrimaryCompanyId = e.PrimaryCompanyId,
                PrimaryDepartmentId = e.PrimaryDepartmentId,
                PrimaryPositionId = e.PrimaryPositionId,
                CreatedAtUtc = e.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<EmployeeSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
