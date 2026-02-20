using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.Modules.Organization.Application.DTOs;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Queries.GetCompanyById;

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
        // Employee accounts can only access their own company
        var accountType = _executionContext.GetClaimValue("AccountType");
        if (accountType == "Employee")
        {
            var companyIdClaim = _executionContext.GetClaimValue("CompanyId");
            if (!Guid.TryParse(companyIdClaim, out var employeeCompanyId))
                return null;

            if (request.CompanyId != employeeCompanyId)
                return null;
        }

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
}
