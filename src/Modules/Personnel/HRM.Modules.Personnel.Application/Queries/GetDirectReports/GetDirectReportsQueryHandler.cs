using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Application.Pagination;
using HRM.Modules.Personnel.Application.Abstractions.Data;
using HRM.Modules.Personnel.Application.Queries.GetEmployees;
using HRM.Modules.Personnel.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Personnel.Application.Queries.GetDirectReports;

/// <summary>
/// Handler for GetDirectReportsQuery.
///
/// Access rules:
/// - System: sees all direct reports of the given manager.
/// - Employee: sees ONLY direct reports within their own company (from JWT CompanyId claim).
///   No valid claim → no results (query.Where(_ => false)).
/// </summary>
public sealed class GetDirectReportsQueryHandler
    : IQueryHandler<GetDirectReportsQuery, PagedResult<EmployeeSummaryDto>>
{
    private readonly IPersonnelQueryContext _context;
    private readonly IExecutionContext _executionContext;

    public GetDirectReportsQueryHandler(
        IPersonnelQueryContext context,
        IExecutionContext executionContext)
    {
        _context = context;
        _executionContext = executionContext;
    }

    public async Task<PagedResult<EmployeeSummaryDto>> Handle(
        GetDirectReportsQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Employees
            .AsNoTracking()
            .Where(e => e.ManagerId == request.ManagerId);

        // Company scope — Employee accounts restricted to their own company
        if (IsEmployeeAccount())
            query = ApplyEmployeeCompanyScope(query);

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

    /// <summary>
    /// Employee account: ONLY sees direct reports within their own company.
    /// No valid CompanyId claim → no results (query.Where(_ => false)).
    /// </summary>
    private IQueryable<Employee> ApplyEmployeeCompanyScope(IQueryable<Employee> query)
    {
        var companyIdClaim = _executionContext.GetClaimValue("CompanyId");
        if (!Guid.TryParse(companyIdClaim, out var companyId))
            return query.Where(_ => false);

        return query.Where(e => e.PrimaryCompanyId == companyId);
    }

    private bool IsEmployeeAccount() =>
        _executionContext.GetClaimValue("AccountType") == "Employee";
}
