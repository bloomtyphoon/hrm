using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Personnel.Application.Abstractions.Data;
using HRM.Modules.Personnel.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Personnel.Application.Queries.GetEmployeeById;

/// <summary>
/// Handler for GetEmployeeByIdQuery.
///
/// Access rules (resolved via IDataScopeService):
/// - Global (System): can access any employee by ID.
/// - Company scope: can access employees in all assigned companies (multi-company support).
/// - None: returns null (no access).
/// </summary>
public sealed class GetEmployeeByIdQueryHandler
    : IQueryHandler<GetEmployeeByIdQuery, EmployeeDetailDto?>
{
    private readonly IPersonnelQueryContext _context;
    private readonly IDataScopeService _dataScopeService;
    private readonly IExecutionContext _executionContext;

    public GetEmployeeByIdQueryHandler(
        IPersonnelQueryContext context,
        IDataScopeService dataScopeService,
        IExecutionContext executionContext)
    {
        _context = context;
        _dataScopeService = dataScopeService;
        _executionContext = executionContext;
    }

    public async Task<EmployeeDetailDto?> Handle(
        GetEmployeeByIdQuery request,
        CancellationToken cancellationToken)
    {
        var rule = await _dataScopeService.GetScopeRuleAsync(
            _executionContext.UserId, "Personnel.Employee.View", cancellationToken);

        var query = _context.Employees
            .AsNoTracking()
            .Where(e => e.Id == request.EmployeeId);

        query = ApplyScopeRule(query, rule);

        return await query
            .Select(e => new EmployeeDetailDto
            {
                Id = e.Id,
                EmployeeCode = e.EmployeeCode,
                FirstName = e.FirstName,
                LastName = e.LastName,
                FullName = e.FirstName + " " + e.LastName,
                Email = e.Email,
                Phone = e.Phone,
                DateOfBirth = e.DateOfBirth,
                HireDate = e.HireDate,
                TerminationDate = e.TerminationDate,
                Status = e.Status,
                ManagerId = e.ManagerId,
                PrimaryCompanyId = e.PrimaryCompanyId,
                PrimaryDepartmentId = e.PrimaryDepartmentId,
                PrimaryPositionId = e.PrimaryPositionId,
                CreatedAtUtc = e.CreatedAtUtc,
                ModifiedAtUtc = e.ModifiedAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken);
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
