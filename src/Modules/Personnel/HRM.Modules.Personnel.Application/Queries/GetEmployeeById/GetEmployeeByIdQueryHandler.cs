using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.Modules.Personnel.Application.Abstractions.Data;
using HRM.Modules.Personnel.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Personnel.Application.Queries.GetEmployeeById;

/// <summary>
/// Handler for GetEmployeeByIdQuery.
///
/// Access rules:
/// - System: can access any employee by ID.
/// - Employee: can only access employees from their own company (from JWT CompanyId claim).
///   No valid claim → returns null (no results).
/// </summary>
public sealed class GetEmployeeByIdQueryHandler
    : IQueryHandler<GetEmployeeByIdQuery, EmployeeDetailDto?>
{
    private readonly IPersonnelQueryContext _context;
    private readonly IExecutionContext _executionContext;

    public GetEmployeeByIdQueryHandler(
        IPersonnelQueryContext context,
        IExecutionContext executionContext)
    {
        _context = context;
        _executionContext = executionContext;
    }

    public async Task<EmployeeDetailDto?> Handle(
        GetEmployeeByIdQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Employees
            .AsNoTracking()
            .Where(e => e.Id == request.EmployeeId);

        // Company scope — Employee accounts restricted to their own company
        if (IsEmployeeAccount())
            query = ApplyEmployeeCompanyScope(query);

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

    /// <summary>
    /// Employee account: ONLY sees employees in their own company.
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
