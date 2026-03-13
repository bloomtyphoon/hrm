using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Organization.Application.Security;
using HRM.Modules.Organization.Domain.Entities;
using HRM.Modules.Organization.Domain.Errors;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Commands.DeactivateCompany;

internal sealed class DeactivateCompanyCommandHandler : ICommandHandler<DeactivateCompanyCommand>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IDataScopeService _dataScopeService;
    private readonly IExecutionContext _executionContext;

    public DeactivateCompanyCommandHandler(
        ICompanyRepository companyRepository,
        IDataScopeService dataScopeService,
        IExecutionContext executionContext)
    {
        _companyRepository = companyRepository;
        _dataScopeService = dataScopeService;
        _executionContext = executionContext;
    }

    public async Task<Result> Handle(DeactivateCompanyCommand request, CancellationToken cancellationToken)
    {
        var company = await _companyRepository.GetByIdAsync(request.CompanyId, cancellationToken);
        if (company is null)
        {
            return Result.Failure(CompanyErrors.NotFound(request.CompanyId));
        }

        var rule = await _dataScopeService.GetCompanyScopeRuleAsync(
            _executionContext.UserId, OrganizationPermissions.Company.Update, cancellationToken);

        if (!CanAccessCompany(request.CompanyId, rule))
        {
            return Result.Failure(new ForbiddenError(
                "Company.AccessDenied", "You do not have permission to deactivate this company."));
        }

        if (company.Status == CompanyStatus.Inactive)
        {
            return Result.Failure(CompanyErrors.AlreadyInactive(company.Code));
        }

        company.Deactivate();
        _companyRepository.Update(company);

        return Result.Success();
    }

    private static bool CanAccessCompany(Guid companyId, DataScopeRule rule) => rule.Level switch
    {
        DataScopeLevel.Global => true,
        DataScopeLevel.Company => rule.DimensionIds.Contains(companyId),
        _ => false
    };
}
