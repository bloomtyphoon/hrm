using HRM.BuildingBlocks.Application.Abstractions.Authentication;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Application.Abstractions.Multitenancy;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Organization.Application.Security;
using HRM.Modules.Organization.Domain.Entities;
using HRM.Modules.Organization.Domain.Errors;
using HRM.Modules.Organization.Domain.Repositories;

namespace HRM.Modules.Organization.Application.Commands.CreateCompany;

internal sealed class CreateCompanyCommandHandler : ICommandHandler<CreateCompanyCommand, Guid>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly ITenantContext _tenantContext;
    private readonly IDataScopeService _dataScopeService;
    private readonly IExecutionContext _executionContext;

    public CreateCompanyCommandHandler(
        ICompanyRepository companyRepository,
        ITenantContext tenantContext,
        IDataScopeService dataScopeService,
        IExecutionContext executionContext)
    {
        _companyRepository = companyRepository;
        _tenantContext = tenantContext;
        _dataScopeService = dataScopeService;
        _executionContext = executionContext;
    }

    public async Task<Result<Guid>> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
    {
        // Scope check: creating a company requires Global scope
        var rule = await _dataScopeService.GetCompanyScopeRuleAsync(
            _executionContext.UserId, OrganizationPermissions.Company.Create, cancellationToken);

        if (rule.Level == DataScopeLevel.None)
        {
            return Result.Failure<Guid>(new ForbiddenError(
                "Company.AccessDenied", "You do not have permission to create companies."));
        }

        // 1. Check code uniqueness
        if (await _companyRepository.ExistsByCodeAsync(request.Code, cancellationToken))
        {
            return Result.Failure<Guid>(CompanyErrors.CodeAlreadyExists(request.Code));
        }

        // 2. Create Company aggregate
        var tenantId = _tenantContext.TenantId
            ?? throw new InvalidOperationException("TenantId is required to create a company.");
        var company = Company.Create(
            tenantId: tenantId,
            code: request.Code,
            name: request.Name,
            taxId: request.TaxId
        );

        // 3. Add to repository
        _companyRepository.Add(company);

        return Result.Success(company.Id);
    }
}
