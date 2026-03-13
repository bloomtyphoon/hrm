using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Domain.Errors;
using HRM.Modules.Identity.Domain.Repositories;
using System.Security.Cryptography;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.Modules.Identity.Application.Abstractions.Authentication;
using HRM.Modules.Identity.Application.Abstractions.Data;
using HRM.Modules.Identity.Application.Security;

namespace HRM.Modules.Identity.Application.Commands.EnableTwoFactor;

internal sealed class EnableTwoFactorCommandHandler : ICommandHandler<EnableTwoFactorCommand, EnableTwoFactorResponse>
{
    private readonly IAccountRepository _accountRepository;
    private readonly IDataScopeService _dataScopeService;
    private readonly ICurrentUserService _currentUser;
    private readonly IIdentityQueryContext _queryContext;

    public EnableTwoFactorCommandHandler(
        IAccountRepository accountRepository,
        IDataScopeService dataScopeService,
        ICurrentUserService currentUser,
        IIdentityQueryContext queryContext)
    {
        _accountRepository = accountRepository;
        _dataScopeService = dataScopeService;
        _currentUser = currentUser;
        _queryContext = queryContext;
    }

    public async Task<Result<EnableTwoFactorResponse>> Handle(EnableTwoFactorCommand request, CancellationToken cancellationToken)
    {
        var rule = await _dataScopeService.GetScopeRuleAsync(
            _currentUser.UserId, IdentityPermissions.Account.View, cancellationToken);
        if (!await AccountScopeFilter.IsAccessibleAsync(rule, _currentUser.UserId, request.AccountId, _queryContext, cancellationToken))
        {
            return Result.Failure<EnableTwoFactorResponse>(AccountErrors.NotFound(request.AccountId));
        }

        var account = await _accountRepository.GetByIdAsync(request.AccountId, cancellationToken);

        if (account is null)
        {
            return Result.Failure<EnableTwoFactorResponse>(AccountErrors.NotFound(request.AccountId));
        }

        if (account.IsTwoFactorEnabled)
        {
            return Result.Failure<EnableTwoFactorResponse>(AccountErrors.TwoFactorAlreadyEnabled);
        }

        // Generate a 160-bit (20-byte) TOTP secret key, Base32-encoded
        var secretKey = GenerateTotpSecretKey();

        account.EnableTwoFactor(secretKey);

        return Result.Success(new EnableTwoFactorResponse(SecretKey: secretKey));
    }

    private static string GenerateTotpSecretKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(20);
        return Base32Encode(bytes);
    }

    private static string Base32Encode(byte[] data)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var result = new char[(data.Length * 8 + 4) / 5];
        int buffer = data[0];
        int next = 1;
        int bitsLeft = 8;
        int index = 0;

        while (index < result.Length)
        {
            if (bitsLeft < 5)
            {
                if (next < data.Length)
                {
                    buffer <<= 8;
                    buffer |= data[next++] & 0xFF;
                    bitsLeft += 8;
                }
                else
                {
                    int pad = 5 - bitsLeft;
                    buffer <<= pad;
                    bitsLeft += pad;
                }
            }

            int charIndex = 0x1F & (buffer >> (bitsLeft - 5));
            bitsLeft -= 5;
            result[index++] = alphabet[charIndex];
        }

        return new string(result);
    }
}
