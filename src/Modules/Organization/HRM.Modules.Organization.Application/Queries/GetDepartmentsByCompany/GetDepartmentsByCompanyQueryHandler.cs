using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.Modules.Organization.Application.DTOs;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Queries.GetDepartmentsByCompany;

/// <summary>
/// Handler for GetDepartmentsByCompanyQuery.
///
/// Access rules:
/// - System: can query departments of any company.
/// - Employee: can only query departments of their own assigned company (from JWT CompanyId claim).
/// </summary>
internal sealed class GetDepartmentsByCompanyQueryHandler
    : IQueryHandler<GetDepartmentsByCompanyQuery, IReadOnlyList<DepartmentDto>>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IExecutionContext _executionContext;

    public GetDepartmentsByCompanyQueryHandler(
        IDepartmentRepository departmentRepository,
        IExecutionContext executionContext)
    {
        _departmentRepository = departmentRepository;
        _executionContext = executionContext;
    }

    public async Task<IReadOnlyList<DepartmentDto>> Handle(
        GetDepartmentsByCompanyQuery request,
        CancellationToken cancellationToken)
    {
        // Security boundary: Employee can only access their own company's departments
        if (!CanAccessCompany(request.CompanyId))
            return Array.Empty<DepartmentDto>();

        var departments = await _departmentRepository.GetByCompanyIdAsync(request.CompanyId, cancellationToken);

        return departments.Select(d => new DepartmentDto(
            Id: d.Id,
            Code: d.Code,
            Name: d.Name,
            CompanyId: d.CompanyId,
            ParentDepartmentId: d.ParentDepartmentId,
            ManagerId: d.ManagerId,
            Level: d.Level,
            Status: d.Status.ToString(),
            CreatedAtUtc: d.CreatedAtUtc,
            ModifiedAtUtc: d.ModifiedAtUtc
        )).ToList();
    }

    private bool IsEmployeeAccount() =>
        _executionContext.GetClaimValue("AccountType") == "Employee";

    private Guid? GetEmployeeCompanyId()
    {
        var claim = _executionContext.GetClaimValue("CompanyId");
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    /// <summary>
    /// System accounts can access any company.
    /// Employee accounts can only access their assigned company.
    /// </summary>
    private bool CanAccessCompany(Guid companyId)
    {
        if (!IsEmployeeAccount()) return true;
        return GetEmployeeCompanyId() == companyId;
    }
}
