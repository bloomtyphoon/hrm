using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Organization.Application.Security;
using HRM.Modules.Organization.Domain.Entities;
using HRM.Modules.Organization.Domain.Errors;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Commands.ActivateCompany;

internal sealed class ActivateCompanyCommandHandler : ICommandHandler<ActivateCompanyCommand>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IDataScopeService _dataScopeService;
    private readonly IExecutionContext _executionContext;

    public ActivateCompanyCommandHandler(
        ICompanyRepository companyRepository,
        IDataScopeService dataScopeService,
        IExecutionContext executionContext)
    {
        _companyRepository = companyRepository;
        _dataScopeService = dataScopeService;
        _executionContext = executionContext;
    }

    public async Task<Result> Handle(ActivateCompanyCommand request, CancellationToken cancellationToken)
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
                "Company.AccessDenied", "You do not have permission to activate this company."));
        }

        if (company.Status == CompanyStatus.Active)
        {
            return Result.Failure(CompanyErrors.AlreadyActive(company.Code));
        }

        company.Activate();
        _companyRepository.Update(company);

        return Result.Success();
    }

    private static bool CanAccessCompany(Guid companyId, DataScopeRule rule) => rule.Level.Category switch
    {
        ScopeCategory.Global => true,
        ScopeCategory.Dimension => rule.DimensionIds.Contains(companyId),
        _ => false
    };
}
