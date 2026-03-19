using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Identity.Domain.Entities;
using HRM.Modules.Identity.Domain.Errors;
using HRM.Modules.Identity.Domain.Repositories;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.Modules.Identity.Application.Abstractions.Authentication;
using HRM.Modules.Identity.Application.Abstractions.Data;
using HRM.Modules.Identity.Application.Security;

namespace HRM.Modules.Identity.Application.Commands.CreateEmployeeProfile;

internal sealed class CreateEmployeeProfileCommandHandler : ICommandHandler<CreateEmployeeProfileCommand, Guid>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IEmployeeProfileRepository _employeeProfileRepository;
    private readonly IDataScopeService _dataScopeService;
    private readonly ICurrentUserService _currentUser;
    private readonly IIdentityQueryContext _queryContext;

    public CreateEmployeeProfileCommandHandler(
        IAccountRepository accountRepository,
        IEmployeeProfileRepository employeeProfileRepository,
        IDataScopeService dataScopeService,
        ICurrentUserService currentUser,
        IIdentityQueryContext queryContext)
    {
        _accountRepository = accountRepository;
        _employeeProfileRepository = employeeProfileRepository;
        _dataScopeService = dataScopeService;
        _currentUser = currentUser;
        _queryContext = queryContext;
    }

    public async Task<Result<Guid>> Handle(CreateEmployeeProfileCommand request, CancellationToken cancellationToken)
    {
        var rule = await _dataScopeService.GetScopeRuleAsync(
            _currentUser.UserId, IdentityPermissions.Account.View, cancellationToken);
        if (!await AccountScopeFilter.IsAccessibleAsync(rule, _currentUser.UserId, request.AccountId, _queryContext, cancellationToken))
        {
            return Result.Failure<Guid>(AccountErrors.NotFound(request.AccountId));
        }

        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);

        if (account is null)
        {
            return Result.Failure<Guid>(AccountErrors.NotFound(request.AccountId));
        }

        if (!account.IsEmployeeAccount)
        {
            return Result.Failure<Guid>(ProfileErrors.AccountNotEmployeeType);
        }

        var existingProfile = await _employeeProfileRepository.GetByAccountIdAsync(request.AccountId, cancellationToken);

        if (existingProfile is not null)
        {
            return Result.Failure<Guid>(ProfileErrors.EmployeeProfileAlreadyExists(request.AccountId));
        }

        var profile = EmployeeProfile.Create(
            tenantId: account.TenantId,
            accountId: request.AccountId,
            employeeId: request.EmployeeId,
            defaultScopeLevel: request.DefaultScopeLevel ?? DataScopeLevel.Self,
            primaryCompanyId: request.PrimaryCompanyId,
            primaryDepartmentId: request.PrimaryDepartmentId,
            primaryPositionId: request.PrimaryPositionId,
            companyIds: request.CompanyIds);

        _employeeProfileRepository.Add(profile);

        return Result.Success(profile.Id);
    }
}
