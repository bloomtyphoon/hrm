using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Organization.Domain.Entities;
using HRM.Modules.Organization.Domain.Errors;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Commands.DeactivateCompany;

internal sealed class DeactivateCompanyCommandHandler : ICommandHandler<DeactivateCompanyCommand>
{
    private readonly ICompanyRepository _companyRepository;

    public DeactivateCompanyCommandHandler(ICompanyRepository companyRepository)
    {
        _companyRepository = companyRepository;
    }

    public async Task<Result> Handle(DeactivateCompanyCommand request, CancellationToken cancellationToken)
    {
        var company = await _companyRepository.GetByIdAsync(request.CompanyId, cancellationToken);
        if (company is null)
        {
            return Result.Failure(CompanyErrors.NotFound(request.CompanyId));
        }

        if (company.Status == CompanyStatus.Inactive)
        {
            return Result.Failure(CompanyErrors.AlreadyInactive(company.Code));
        }

        company.Deactivate();
        _companyRepository.Update(company);

        return Result.Success();
    }
}
