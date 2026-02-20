using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.Modules.Organization.Application.DTOs;
using HRM.Modules.Organization.Domain.Entities;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Queries.GetCompanies;

/// <summary>
/// Handler for GetCompaniesQuery.
///
/// Access rules:
/// - System: sees all companies. ActiveOnly filter is respected.
/// - Employee: sees ONLY their assigned company (from JWT CompanyId claim).
///
/// Filtering layers:
/// 1. Account type branch — System vs Employee diverge into separate code paths
/// 2. System path: optional ActiveOnly filter via repository
/// 3. Employee path: single company resolved from JWT claim
/// </summary>
internal sealed class GetCompaniesQueryHandler : IQueryHandler<GetCompaniesQuery, IReadOnlyList<CompanyDto>>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IExecutionContext _executionContext;

    public GetCompaniesQueryHandler(
        ICompanyRepository companyRepository,
        IExecutionContext executionContext)
    {
        _companyRepository = companyRepository;
        _executionContext = executionContext;
    }

    public async Task<IReadOnlyList<CompanyDto>> Handle(GetCompaniesQuery request, CancellationToken cancellationToken)
    {
        return IsEmployeeAccount()
            ? await GetEmployeeVisibleCompaniesAsync(cancellationToken)
            : await GetSystemCompaniesAsync(request, cancellationToken);
    }

    /// <summary>
    /// Employee account: sees only their assigned company from JWT CompanyId claim.
    /// </summary>
    private async Task<IReadOnlyList<CompanyDto>> GetEmployeeVisibleCompaniesAsync(CancellationToken cancellationToken)
    {
        var companyId = GetEmployeeCompanyId();
        if (!companyId.HasValue)
            return Array.Empty<CompanyDto>();

        var company = await _companyRepository.GetByIdAsync(companyId.Value, cancellationToken);
        return company is null ? Array.Empty<CompanyDto>() : [MapToDto(company)];
    }

    /// <summary>
    /// System account: sees all companies, with optional ActiveOnly filter.
    /// </summary>
    private async Task<IReadOnlyList<CompanyDto>> GetSystemCompaniesAsync(
        GetCompaniesQuery request,
        CancellationToken cancellationToken)
    {
        var companies = request.ActiveOnly == true
            ? await _companyRepository.GetActiveAsync(cancellationToken)
            : await _companyRepository.GetAllAsync(cancellationToken);

        return companies.Select(MapToDto).ToList();
    }

    private bool IsEmployeeAccount() =>
        _executionContext.GetClaimValue("AccountType") == "Employee";

    private Guid? GetEmployeeCompanyId()
    {
        var claim = _executionContext.GetClaimValue("CompanyId");
        return Guid.TryParse(claim, out var id) ? id : null;
    }

    private static CompanyDto MapToDto(Company company) => new(
        Id: company.Id,
        Code: company.Code,
        Name: company.Name,
        TaxId: company.TaxId,
        Status: company.Status.ToString(),
        CreatedAtUtc: company.CreatedAtUtc,
        ModifiedAtUtc: company.ModifiedAtUtc);
}
