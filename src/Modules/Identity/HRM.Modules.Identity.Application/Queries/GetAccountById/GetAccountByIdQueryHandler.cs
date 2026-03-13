using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Application.Abstractions.Data;
using HRM.Modules.Identity.Domain.Errors;
using Microsoft.EntityFrameworkCore;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.Modules.Identity.Application.Abstractions.Authentication;
using HRM.Modules.Identity.Application.Security;

namespace HRM.Modules.Identity.Application.Queries.GetAccountById;

/// <summary>
/// Handler for GetAccountByIdQuery.
/// Applies visibility check — Employee accounts can only view
/// accounts within their assigned companies.
/// </summary>
public sealed class GetAccountByIdQueryHandler
    : IQueryHandler<GetAccountByIdQuery, Result<AccountDetailDto>>
{
    private readonly IIdentityQueryContext _context;
    private readonly IDataScopeService _dataScopeService;
    private readonly ICurrentUserService _currentUser;

    public GetAccountByIdQueryHandler(
        IIdentityQueryContext context,
        IDataScopeService dataScopeService,
        ICurrentUserService currentUser)
    {
        _context = context;
        _dataScopeService = dataScopeService;
        _currentUser = currentUser;
    }

    public async Task<Result<AccountDetailDto>> Handle(
        GetAccountByIdQuery request,
        CancellationToken cancellationToken)
    {
        var rule = await _dataScopeService.GetScopeRuleAsync(
            _currentUser.UserId, IdentityPermissions.Account.View, cancellationToken);
        if (!await AccountScopeFilter.IsAccessibleAsync(rule, _currentUser.UserId, request.AccountId, _context, cancellationToken))
        {
            return Result.Failure<AccountDetailDto>(AccountErrors.NotFound(request.AccountId));
        }

        var account = await _context.Accounts
            .AsNoTracking()
            .Where(a => a.Id == request.AccountId)
            .Select(a => new AccountDetailDto
            {
                Id = a.Id,
                Username = a.Username,
                Email = a.Email,
                FullName = a.FullName,
                PhoneNumber = a.PhoneNumber,
                Status = a.Status,
                AccountType = a.AccountType,
                IsTwoFactorEnabled = a.IsTwoFactorEnabled,
                ActivatedAtUtc = a.ActivatedAtUtc,
                LastLoginAtUtc = a.LastLoginAtUtc,
                CreatedAtUtc = a.CreatedAtUtc,
                ModifiedAtUtc = a.ModifiedAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (account is null)
        {
            return Result.Failure<AccountDetailDto>(AccountErrors.NotFound(request.AccountId));
        }

        return Result.Success(account);
    }
}
