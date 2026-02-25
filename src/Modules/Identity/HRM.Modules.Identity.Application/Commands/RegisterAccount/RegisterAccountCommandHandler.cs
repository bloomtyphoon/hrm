using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Application.Abstractions.Authentication;
using HRM.Modules.Identity.Domain.Entities;
using HRM.Modules.Identity.Domain.Enums;
using HRM.Modules.Identity.Domain.Errors;
using HRM.Modules.Identity.Domain.Repositories;

namespace HRM.Modules.Identity.Application.Commands.RegisterAccount;

/// <summary>
/// Handler for RegisterAccountCommand.
/// Creates new account in Pending status.
/// </summary>
internal sealed class RegisterAccountCommandHandler : ICommandHandler<RegisterAccountCommand, Guid>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterAccountCommandHandler(
        IAccountRepository accountRepository,
        IPasswordHasher passwordHasher)
    {
        _accountRepository = accountRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<Guid>> Handle(RegisterAccountCommand request, CancellationToken cancellationToken)
    {
        // 1. Check username uniqueness
        if (await _accountRepository.ExistsByUsernameAsync(request.Username, cancellationToken))
        {
            return Result.Failure<Guid>(AccountErrors.UsernameAlreadyExists(request.Username));
        }

        // 2. Check email uniqueness
        if (await _accountRepository.ExistsByEmailAsync(request.Email, cancellationToken))
        {
            return Result.Failure<Guid>(AccountErrors.EmailAlreadyExists(request.Email));
        }

        // 3. Hash password with BCrypt
        var passwordHash = _passwordHasher.HashPassword(request.Password);

        // 4. Create Account aggregate using the correct factory based on AccountType
        var account = request.AccountType switch
        {
            AccountType.System => Account.CreateSystemAccount(
                username: request.Username,
                email: request.Email,
                passwordHash: passwordHash,
                fullName: request.FullName,
                phoneNumber: request.PhoneNumber),
            AccountType.Employee => Account.CreateEmployeeAccount(
                username: request.Username,
                email: request.Email,
                passwordHash: passwordHash,
                fullName: request.FullName,
                phoneNumber: request.PhoneNumber),
            _ => throw new ArgumentOutOfRangeException(nameof(request.AccountType), request.AccountType, "Unsupported account type.")
        };

        // 5. Add to repository
        _accountRepository.Add(account);

        // 6. Return account ID
        return Result.Success(account.Id);
    }
}
