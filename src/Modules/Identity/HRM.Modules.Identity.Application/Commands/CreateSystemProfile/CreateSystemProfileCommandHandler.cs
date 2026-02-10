using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Domain.Entities;
using HRM.Modules.Identity.Domain.Errors;
using HRM.Modules.Identity.Domain.Repositories;

namespace HRM.Modules.Identity.Application.Commands.CreateSystemProfile;

internal sealed class CreateSystemProfileCommandHandler : ICommandHandler<CreateSystemProfileCommand, Guid>
{
    private readonly IAccountRepository _accountRepository;
    private readonly ISystemProfileRepository _systemProfileRepository;

    public CreateSystemProfileCommandHandler(
        IAccountRepository accountRepository,
        ISystemProfileRepository systemProfileRepository)
    {
        _accountRepository = accountRepository;
        _systemProfileRepository = systemProfileRepository;
    }

    public async Task<Result<Guid>> Handle(CreateSystemProfileCommand request, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);

        if (account is null)
        {
            return Result.Failure<Guid>(AccountErrors.NotFound(request.AccountId));
        }

        if (!account.IsSystemAccount)
        {
            return Result.Failure<Guid>(ProfileErrors.AccountNotSystemType);
        }

        var existingProfile = await _systemProfileRepository.GetByAccountIdAsync(request.AccountId, cancellationToken);

        if (existingProfile is not null)
        {
            return Result.Failure<Guid>(ProfileErrors.SystemProfileAlreadyExists(request.AccountId));
        }

        var profile = SystemProfile.Create(
            accountId: request.AccountId,
            isSuperAdmin: request.IsSuperAdmin,
            department: request.Department,
            jobTitle: request.JobTitle);

        _systemProfileRepository.Add(profile);

        return Result.Success(profile.Id);
    }
}
