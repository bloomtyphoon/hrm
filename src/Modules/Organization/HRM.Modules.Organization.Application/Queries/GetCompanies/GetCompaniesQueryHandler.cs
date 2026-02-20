using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.Modules.Organization.Application.DTOs;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Queries.GetCompanies;

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
        // Employee accounts can only see their own company
        var accountType = _executionContext.GetClaimValue("AccountType");
        if (accountType == "Employee")
        {
            var companyIdClaim = _executionContext.GetClaimValue("CompanyId");
            if (!Guid.TryParse(companyIdClaim, out var employeeCompanyId))
                return Array.Empty<CompanyDto>();

            var company = await _companyRepository.GetByIdAsync(employeeCompanyId, cancellationToken);
            if (company is null)
                return Array.Empty<CompanyDto>();

            return new[]
            {
                new CompanyDto(
                    Id: company.Id,
                    Code: company.Code,
                    Name: company.Name,
                    TaxId: company.TaxId,
                    Status: company.Status.ToString(),
                    CreatedAtUtc: company.CreatedAtUtc,
                    ModifiedAtUtc: company.ModifiedAtUtc)
            };
        }

        // System accounts see all companies
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
