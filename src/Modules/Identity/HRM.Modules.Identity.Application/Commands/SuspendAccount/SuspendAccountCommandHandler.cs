using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Domain.Errors;
using HRM.Modules.Identity.Domain.Repositories;

namespace HRM.Modules.Identity.Application.Commands.SuspendAccount;

internal sealed class SuspendAccountCommandHandler : ICommandHandler<SuspendAccountCommand>
{
    private readonly IAccountRepository _accountRepository;

    public SuspendAccountCommandHandler(IAccountRepository accountRepository)
    {
        _accountRepository = accountRepository;
    }

    public async Task<Result> Handle(SuspendAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);

        if (account is null)
        {
            return Result.Failure(AccountErrors.NotFound(request.AccountId));
        }

        account.Suspend();

        return Result.Success();
    }
}
