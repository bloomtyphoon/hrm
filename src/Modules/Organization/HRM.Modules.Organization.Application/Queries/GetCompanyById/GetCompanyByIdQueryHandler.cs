using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Organization.Application.DTOs;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Queries.GetCompanyById;

/// <summary>
/// Handler for GetCompanyByIdQuery.
///
/// Access rules (resolved via IDataScopeService):
/// - Global (System): can access any company by ID.
/// - Company scope: can access companies in the assigned list (multi-company support).
/// - None: returns null.
/// </summary>
internal sealed class GetCompanyByIdQueryHandler : IQueryHandler<GetCompanyByIdQuery, CompanyDto?>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IDataScopeService _dataScopeService;
    private readonly IExecutionContext _executionContext;

    public GetCompanyByIdQueryHandler(
        ICompanyRepository companyRepository,
        IDataScopeService dataScopeService,
        IExecutionContext executionContext)
    {
        _companyRepository = companyRepository;
        _dataScopeService = dataScopeService;
        _executionContext = executionContext;
    }

    public async Task<CompanyDto?> Handle(GetCompanyByIdQuery request, CancellationToken cancellationToken)
    {
        var rule = await _dataScopeService.GetScopeRuleAsync(
            _executionContext.UserId, "Organization.Company.View", cancellationToken);

        if (!CanAccessCompany(request.CompanyId, rule))
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

    private static bool CanAccessCompany(Guid companyId, DataScopeRule rule) => rule.Level switch
    {
        DataScopeLevel.Global => true,
        DataScopeLevel.Company => rule.DimensionIds.Contains(companyId),
        _ => false
    };
}
