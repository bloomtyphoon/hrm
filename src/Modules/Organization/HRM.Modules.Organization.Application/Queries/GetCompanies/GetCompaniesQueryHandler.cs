using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.Modules.Organization.Application.DTOs;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Queries.GetCompanies;

internal sealed class GetCompaniesQueryHandler : IQueryHandler<GetCompaniesQuery, IReadOnlyList<CompanyDto>>
{
    private readonly ICompanyRepository _companyRepository;

    public GetCompaniesQueryHandler(ICompanyRepository companyRepository)
    {
        _companyRepository = companyRepository;
    }

    public async Task<IReadOnlyList<CompanyDto>> Handle(GetCompaniesQuery request, CancellationToken cancellationToken)
    {
        var companies = request.ActiveOnly == true
            ? await _companyRepository.GetActiveAsync(cancellationToken)
            : await _companyRepository.GetAllAsync(cancellationToken);

        return companies.Select(company => new CompanyDto(
            Id: company.Id,
            Code: company.Code,
            Name: company.Name,
            TaxId: company.TaxId,
            Status: company.Status.ToString(),
            CreatedAtUtc: company.CreatedAtUtc,
            ModifiedAtUtc: company.ModifiedAtUtc
        )).ToList();
    }
}
