using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.Modules.Organization.Application.DTOs;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Queries.GetCompanyById;

/// <summary>
/// Handler for GetCompanyByIdQuery.
///
/// Access rules:
/// - System: can access any company by ID.
/// - Employee: can only access their own assigned company (from JWT CompanyId claim).
/// </summary>
internal sealed class GetCompanyByIdQueryHandler : IQueryHandler<GetCompanyByIdQuery, CompanyDto?>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IExecutionContext _executionContext;

    public GetCompanyByIdQueryHandler(
        ICompanyRepository companyRepository,
        IExecutionContext executionContext)
    {
        _companyRepository = companyRepository;
        _executionContext = executionContext;
    }

    public async Task<CompanyDto?> Handle(GetCompanyByIdQuery request, CancellationToken cancellationToken)
    {
        // Security boundary: Employee can only access their own company
        if (!CanAccessCompany(request.CompanyId))
            return null;

        var company = await _companyRepository.GetByIdAsync(request.CompanyId, cancellationToken);

        if (company is null)
            return null;

        return new CompanyDto(
            Id: company.Id,
            Code: company.Code,
            Name: company.Name,
            TaxId: company.TaxId,
            Status: company.Status.ToString(),
            CreatedAtUtc: company.CreatedAtUtc,
            ModifiedAtUtc: company.ModifiedAtUtc
        );
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
