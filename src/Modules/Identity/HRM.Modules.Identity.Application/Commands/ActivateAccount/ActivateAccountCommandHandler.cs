using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Domain.Errors;
using HRM.Modules.Identity.Domain.Repositories;

namespace HRM.Modules.Identity.Application.Commands.ActivateAccount;

/// <summary>
/// Handler for ActivateAccountCommand.
/// Changes account status from Pending to Active.
/// </summary>
internal sealed class ActivateAccountCommandHandler : ICommandHandler<ActivateAccountCommand>
{
    private readonly IAccountRepository _accountRepository;

    public ActivateAccountCommandHandler(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<Result> Handle(ActivateAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);

        if (account is null)
        {
            return Result.Failure(AccountErrors.NotFound(request.AccountId));
        }

        account.Activate();

        return Result.Success();
    }
}
