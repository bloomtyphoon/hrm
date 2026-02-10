using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Domain.Entities;
using HRM.Modules.Identity.Domain.Errors;
using HRM.Modules.Identity.Domain.Repositories;

namespace HRM.Modules.Identity.Application.Commands.CreateEmployeeProfile;

internal sealed class CreateEmployeeProfileCommandHandler : ICommandHandler<CreateEmployeeProfileCommand, Guid>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IEmployeeProfileRepository _employeeProfileRepository;

    public CreateEmployeeProfileCommandHandler(
        IAccountRepository accountRepository,
        IEmployeeProfileRepository employeeProfileRepository)
    {
        _accountRepository = accountRepository;
        _employeeProfileRepository = employeeProfileRepository;
    }

    public async Task<Result<Guid>> Handle(CreateEmployeeProfileCommand request, CancellationToken cancellationToken)
    {
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
            accountId: request.AccountId,
            employeeId: request.EmployeeId,
            defaultScopeLevel: request.DefaultScopeLevel,
            primaryCompanyId: request.PrimaryCompanyId,
            primaryDepartmentId: request.PrimaryDepartmentId,
            primaryPositionId: request.PrimaryPositionId);

        _employeeProfileRepository.Add(profile);

        return Result.Success(profile.Id);
    }
}
