using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Organization.Application.DTOs;
using HRM.Modules.Organization.Application.Security;
using HRM.Modules.Organization.Domain.Entities;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Queries.GetCompanies;

/// <summary>
/// Handler for GetCompaniesQuery.
///
/// Access rules (resolved via IDataScopeService):
/// - Global (System): sees all companies. ActiveOnly filter is respected.
/// - Company scope: sees all assigned companies (multi-company support).
/// - None: empty result.
/// </summary>
internal sealed class GetCompaniesQueryHandler : IQueryHandler<GetCompaniesQuery, IReadOnlyList<CompanyDto>>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IDataScopeService _dataScopeService;
    private readonly IExecutionContext _executionContext;

    public GetCompaniesQueryHandler(
        ICompanyRepository companyRepository,
        IDataScopeService dataScopeService,
        IExecutionContext executionContext)
    {
        _companyRepository = companyRepository;
        _dataScopeService = dataScopeService;
        _executionContext = executionContext;
    }

    public async Task<IReadOnlyList<CompanyDto>> Handle(GetCompaniesQuery request, CancellationToken cancellationToken)
    {
        var rule = await _dataScopeService.GetScopeRuleAsync(
            _executionContext.UserId, OrganizationPermissions.Company.View, cancellationToken);

        return rule.Level switch
        {
            DataScopeLevel.Global => await GetAllCompaniesAsync(request, cancellationToken),
            DataScopeLevel.Company => await GetAssignedCompaniesAsync(rule.DimensionIds, cancellationToken),
            _ => Array.Empty<CompanyDto>()
        };
    }

    private async Task<IReadOnlyList<CompanyDto>> GetAllCompaniesAsync(
        GetCompaniesQuery request,
        CancellationToken cancellationToken)
    {
        var companies = request.ActiveOnly == true
            ? await _companyRepository.GetActiveAsync(cancellationToken)
            : await _companyRepository.GetAllAsync(cancellationToken);

        return companies.Select(MapToDto).ToList();
    }

    private async Task<IReadOnlyList<CompanyDto>> GetAssignedCompaniesAsync(
        IReadOnlyCollection<Guid> companyIds,
        CancellationToken cancellationToken)
    {
        var companies = await _companyRepository.GetByIdsAsync(companyIds, cancellationToken);
        return companies.Select(MapToDto).ToList();
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
