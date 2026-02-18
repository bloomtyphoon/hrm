using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Organization.Domain.Entities;
using HRM.Modules.Organization.Domain.Errors;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Commands.ActivateCompany;

internal sealed class ActivateCompanyCommandHandler : ICommandHandler<ActivateCompanyCommand>
{
    private readonly ICompanyRepository _companyRepository;

    public ActivateCompanyCommandHandler(ICompanyRepository companyRepository)
    {
        _companyRepository = companyRepository;
    }

    public async Task<Result> Handle(ActivateCompanyCommand request, CancellationToken cancellationToken)
    {
        var company = await _companyRepository.GetByIdAsync(request.CompanyId, cancellationToken);
        if (company is null)
        {
            return Result.Failure(CompanyErrors.NotFound(request.CompanyId));
        }

        if (company.Status == CompanyStatus.Active)
        {
            return Result.Failure(CompanyErrors.AlreadyActive(company.Code));
        }

        company.Activate();
        _companyRepository.Update(company);

        return Result.Success();
    }
}
