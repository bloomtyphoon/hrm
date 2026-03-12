using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Personnel.Application.Abstractions.Data;
using HRM.Modules.Personnel.Application.Security;
using HRM.Modules.Personnel.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Personnel.Application.Queries.GetEmployeeById;

/// <summary>
/// Handler for GetEmployeeByIdQuery.
///
/// Access rules (resolved via IDataScopeService):
/// - Global (System): can access any employee by ID.
/// - Company/Department/Position scope: can access employees with matching active assignments.
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
            _executionContext.UserId, PersonnelPermissions.Employee.View, cancellationToken);

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

    private IQueryable<Employee> ApplyScopeRule(IQueryable<Employee> query, DataScopeRule rule)
    {
        return rule.Level switch
        {
            DataScopeLevel.Global => query,
            DataScopeLevel.None => query.Where(_ => false),
            DataScopeLevel.Self => query.Where(e => e.OwnerId == rule.SelfEmployeeId!.Value),
            DataScopeLevel.DirectReports => query.Where(e => rule.EmployeeIds.Contains(e.OwnerId)),
            DataScopeLevel.EmployeeSet => query.Where(e => rule.EmployeeIds.Contains(e.OwnerId)),
            DataScopeLevel.Company => BuildDimensionFilter(query, rule.DimensionIds, DataScopeLevel.Company),
            DataScopeLevel.Department => BuildDimensionFilter(query, rule.DimensionIds, DataScopeLevel.Department),
            DataScopeLevel.Position => BuildDimensionFilter(query, rule.DimensionIds, DataScopeLevel.Position),
            _ => query.Where(_ => false)
        };
    }

    private IQueryable<Employee> BuildDimensionFilter(
        IQueryable<Employee> query,
        IReadOnlyCollection<Guid> dimensionIds,
        DataScopeLevel level)
    {
        var ids = dimensionIds.ToList();

        var employeeIdsWithAccess = level switch
        {
            DataScopeLevel.Company => _context.EmployeeAssignments
                .Where(a => a.Status == AssignmentStatus.Active && !a.EndDate.HasValue && ids.Contains(a.CompanyId))
                .Select(a => a.EmployeeId),
            DataScopeLevel.Department => _context.EmployeeAssignments
                .Where(a => a.Status == AssignmentStatus.Active && !a.EndDate.HasValue && ids.Contains(a.DepartmentId))
                .Select(a => a.EmployeeId),
            DataScopeLevel.Position => _context.EmployeeAssignments
                .Where(a => a.Status == AssignmentStatus.Active && !a.EndDate.HasValue && ids.Contains(a.PositionId))
                .Select(a => a.EmployeeId),
            _ => throw new ArgumentOutOfRangeException(nameof(level))
        };

        return query.Where(e => employeeIdsWithAccess.Contains(e.Id));
    }
}
