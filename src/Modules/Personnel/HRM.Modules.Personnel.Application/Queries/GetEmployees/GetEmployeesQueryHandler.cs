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

        query = ApplyScopeRule(query, rule, _context);

        // System accounts (Global): optionally filter by requested CompanyId (via assignments)
        if (rule.Level == DataScopeLevel.Global && request.CompanyId.HasValue)
        {
            var companyId = request.CompanyId.Value;
            query = query.Where(e => e.Assignments.Any(a =>
                a.Status == AssignmentStatus.Active && !a.EndDate.HasValue && a.CompanyId == companyId));
        }

        // Layer 2: Search filter
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var pattern = $"%{request.SearchTerm}%";
            query = query.Where(e =>
                EF.Functions.Like(e.EmployeeCode, pattern) ||
                EF.Functions.Like(e.FirstName, pattern) ||
                EF.Functions.Like(e.LastName, pattern) ||
                EF.Functions.Like(e.Email, pattern));
        }

        // Layer 2: Status filter
        if (request.Status.HasValue)
        {
            query = query.Where(e => e.Status == request.Status.Value);
        }

        // Layer 2: Department filter (via assignments, not primary only)
        if (request.DepartmentId.HasValue)
        {
            var departmentId = request.DepartmentId.Value;
            query = query.Where(e => e.Assignments.Any(a =>
                a.Status == AssignmentStatus.Active && !a.EndDate.HasValue && a.DepartmentId == departmentId));
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
    /// Apply scope rule using Assignments table for dimension-based filtering.
    /// Employees are visible if they have ANY active assignment matching the scope dimensions.
    /// </summary>
    private static IQueryable<Employee> ApplyScopeRule(
        IQueryable<Employee> query, DataScopeRule rule, IPersonnelQueryContext context)
    {
        return rule.Level switch
        {
            DataScopeLevel.Global => query,
            DataScopeLevel.None => query.Where(_ => false),
            DataScopeLevel.Self => query.Where(e => e.OwnerId == rule.SelfEmployeeId!.Value),
            DataScopeLevel.DirectReports => query.Where(e => rule.EmployeeIds.Contains(e.OwnerId)),
            DataScopeLevel.EmployeeSet => query.Where(e => rule.EmployeeIds.Contains(e.OwnerId)),
            DataScopeLevel.Company => BuildDimensionFilter(query, context, rule.DimensionIds, DataScopeLevel.Company),
            DataScopeLevel.Department => BuildDimensionFilter(query, context, rule.DimensionIds, DataScopeLevel.Department),
            DataScopeLevel.Position => BuildDimensionFilter(query, context, rule.DimensionIds, DataScopeLevel.Position),
            _ => query.Where(_ => false)
        };
    }

    /// <summary>
    /// Filter employees via JOIN on active assignments matching dimension IDs.
    /// </summary>
    private static IQueryable<Employee> BuildDimensionFilter(
        IQueryable<Employee> query,
        IPersonnelQueryContext context,
        IReadOnlyCollection<Guid> dimensionIds,
        DataScopeLevel level)
    {
        var ids = dimensionIds.ToList();

        // Employees who have at least one active assignment matching the dimension
        var employeeIdsWithAccess = level switch
        {
            DataScopeLevel.Company => context.EmployeeAssignments
                .Where(a => a.Status == AssignmentStatus.Active && !a.EndDate.HasValue && ids.Contains(a.CompanyId))
                .Select(a => a.EmployeeId),
            DataScopeLevel.Department => context.EmployeeAssignments
                .Where(a => a.Status == AssignmentStatus.Active && !a.EndDate.HasValue && ids.Contains(a.DepartmentId))
                .Select(a => a.EmployeeId),
            DataScopeLevel.Position => context.EmployeeAssignments
                .Where(a => a.Status == AssignmentStatus.Active && !a.EndDate.HasValue && ids.Contains(a.PositionId))
                .Select(a => a.EmployeeId),
            _ => throw new ArgumentOutOfRangeException(nameof(level))
        };

        return query.Where(e => employeeIdsWithAccess.Contains(e.Id));
    }
}
