using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Application.Pagination;
using HRM.Modules.Personnel.Application.Abstractions.Data;
using HRM.Modules.Personnel.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Personnel.Application.Queries.GetEmployees;

/// <summary>
/// Handler for GetEmployeesQuery.
///
/// Access rules:
/// - System: sees all employees. Optional CompanyId filter from request is respected.
/// - Employee: sees ONLY employees in their own company (from JWT CompanyId claim).
///   Request CompanyId is ignored — company scope is always enforced.
///
/// Filtering layers:
/// 1. Company scope — security boundary (different rules per account type)
/// 2. Search/Status/Department/Manager — user-driven refinement
/// </summary>
public sealed class GetEmployeesQueryHandler
    : IQueryHandler<GetEmployeesQuery, PagedResult<EmployeeSummaryDto>>
{
    private readonly IPersonnelQueryContext _context;
    private readonly IExecutionContext _executionContext;

    public GetEmployeesQueryHandler(
        IPersonnelQueryContext context,
        IExecutionContext executionContext)
    {
        _context = context;
        _executionContext = executionContext;
    }

    public async Task<PagedResult<EmployeeSummaryDto>> Handle(
        GetEmployeesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Employees.AsNoTracking();

        // Layer 1: Company scope — different rules per account type
        query = IsEmployeeAccount()
            ? ApplyEmployeeCompanyScope(query)
            : ApplySystemCompanyScope(query, request.CompanyId);

        // Layer 2: Search filter
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(e =>
                e.EmployeeCode.ToLower().Contains(searchTerm) ||
                e.FirstName.ToLower().Contains(searchTerm) ||
                e.LastName.ToLower().Contains(searchTerm) ||
                e.Email.ToLower().Contains(searchTerm));
        }

        // Layer 2: Status filter
        if (request.Status.HasValue)
        {
            query = query.Where(e => e.Status == request.Status.Value);
        }

        // Layer 2: Department filter
        if (request.DepartmentId.HasValue)
        {
            query = query.Where(e => e.PrimaryDepartmentId == request.DepartmentId.Value);
        }

        // Layer 2: Manager filter
        if (request.ManagerId.HasValue)
        {
            query = query.Where(e => e.ManagerId == request.ManagerId.Value);
        }

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
    /// System account: sees all employees.
    /// Optional CompanyId from request filters to a specific company.
    /// </summary>
    private static IQueryable<Employee> ApplySystemCompanyScope(
        IQueryable<Employee> query,
        Guid? requestCompanyId)
    {
        return requestCompanyId.HasValue
            ? query.Where(e => e.PrimaryCompanyId == requestCompanyId.Value)
            : query;
    }

    /// <summary>
    /// Employee account: ONLY sees employees in their own company.
    /// CompanyId is resolved from JWT claim — request filter is ignored.
    /// No valid claim → no results (query.Where(_ => false)).
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
