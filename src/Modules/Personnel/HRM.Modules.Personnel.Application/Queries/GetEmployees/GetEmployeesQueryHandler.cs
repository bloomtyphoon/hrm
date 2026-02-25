using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Application.Pagination;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Personnel.Application.Abstractions.Data;
using HRM.Modules.Personnel.Application.Security;
using HRM.Modules.Personnel.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Personnel.Application.Queries.GetEmployees;

/// <summary>
/// Handler for GetEmployeesQuery.
///
/// Access rules (resolved via IDataScopeService):
/// - Global (System): sees all employees. Optional CompanyId filter from request is respected.
/// - Company scope: sees employees in all assigned companies (multi-company support).
/// - Self / EmployeeSet: sees own or subordinate employee records.
/// - None: no results.
///
/// Filtering layers:
/// 1. Scope rule — security boundary via DataScopeRule
/// 2. Search/Status/Department/Manager — user-driven refinement
/// </summary>
public sealed class GetEmployeesQueryHandler
    : IQueryHandler<GetEmployeesQuery, PagedResult<EmployeeSummaryDto>>
{
    private readonly IPersonnelQueryContext _context;
    private readonly IDataScopeService _dataScopeService;
    private readonly IExecutionContext _executionContext;

    public GetEmployeesQueryHandler(
        IPersonnelQueryContext context,
        IDataScopeService dataScopeService,
        IExecutionContext executionContext)
    {
        _context = context;
        _dataScopeService = dataScopeService;
        _executionContext = executionContext;
    }

    public async Task<PagedResult<EmployeeSummaryDto>> Handle(
        GetEmployeesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Employees.AsNoTracking();

        // Layer 1: Scope rule — security boundary
        var rule = await _dataScopeService.GetScopeRuleAsync(
            _executionContext.UserId, PersonnelPermissions.Employee.View, cancellationToken);

        query = ApplyScopeRule(query, rule);

        // System accounts (Global): optionally filter by requested CompanyId
        if (rule.Level == DataScopeLevel.Global && request.CompanyId.HasValue)
            query = query.Where(e => e.PrimaryCompanyId == request.CompanyId.Value);

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

    private static IQueryable<Employee> ApplyScopeRule(IQueryable<Employee> query, DataScopeRule rule)
    {
        return rule.Level switch
        {
            DataScopeLevel.Global => query,
            DataScopeLevel.None => query.Where(_ => false),
            DataScopeLevel.Self => query.Where(e => e.OwnerId == rule.SelfEmployeeId!.Value),
            DataScopeLevel.EmployeeSet => query.Where(e => rule.EmployeeIds.Contains(e.OwnerId)),
            DataScopeLevel.Company => BuildCompanyFilter(query, rule.DimensionIds),
            _ => query.Where(_ => false)
        };
    }

    private static IQueryable<Employee> BuildCompanyFilter(
        IQueryable<Employee> query,
        IReadOnlyCollection<Guid> companyIds)
    {
        var ids = companyIds.ToList();
        return query.Where(e => e.PrimaryCompanyId != null && ids.Contains(e.PrimaryCompanyId.Value));
    }
}
