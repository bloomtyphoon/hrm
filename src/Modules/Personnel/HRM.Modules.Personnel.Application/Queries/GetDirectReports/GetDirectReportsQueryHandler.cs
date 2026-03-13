using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Application.Pagination;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Personnel.Application.Abstractions.Data;
using HRM.Modules.Personnel.Application.Queries.GetEmployees;
using HRM.Modules.Personnel.Application.Security;
using HRM.Modules.Personnel.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Personnel.Application.Queries.GetDirectReports;

/// <summary>
/// Handler for GetDirectReportsQuery.
///
/// Access rules (resolved via IDataScopeService):
/// - Global (System): sees all direct reports of the given manager.
/// - Company/Department/Position scope: sees direct reports with matching active assignments.
/// - None: no results.
/// </summary>
public sealed class GetDirectReportsQueryHandler
    : IQueryHandler<GetDirectReportsQuery, PagedResult<EmployeeSummaryDto>>
{
    private readonly IPersonnelQueryContext _context;
    private readonly IDataScopeService _dataScopeService;
    private readonly IExecutionContext _executionContext;

    public GetDirectReportsQueryHandler(
        IPersonnelQueryContext context,
        IDataScopeService dataScopeService,
        IExecutionContext executionContext)
    {
        _context = context;
        _dataScopeService = dataScopeService;
        _executionContext = executionContext;
    }

    public async Task<PagedResult<EmployeeSummaryDto>> Handle(
        GetDirectReportsQuery request,
        CancellationToken cancellationToken)
    {
        var rule = await _dataScopeService.GetScopeRuleAsync(
            _executionContext.UserId, PersonnelPermissions.Employee.View, cancellationToken);

        var query = _context.Employees
            .AsNoTracking()
            .Where(e => e.ManagerId == request.ManagerId);

        query = EmployeeScopeFilter.ApplyScope(query, rule, _context);

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
