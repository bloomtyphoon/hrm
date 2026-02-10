using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Domain.Errors;
using HRM.Modules.Identity.Domain.Repositories;

namespace HRM.Modules.Identity.Application.Commands.DisableTwoFactor;

internal sealed class DisableTwoFactorCommandHandler : ICommandHandler<DisableTwoFactorCommand>
{
    private readonly IAccountRepository _accountRepository;

    public DisableTwoFactorCommandHandler(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<Result> Handle(DisableTwoFactorCommand request, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);

        if (account is null)
        {
            return Result.Failure(AccountErrors.NotFound(request.AccountId));
        }

        if (!account.IsTwoFactorEnabled)
        {
            return Result.Failure(AccountErrors.TwoFactorNotEnabled);
        }

        account.DisableTwoFactor();

        return Result.Success();
    }
}
