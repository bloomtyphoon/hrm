using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Application.Abstractions.Authorization;
using HRM.Modules.Identity.Application.Abstractions.Data;
using HRM.Modules.Identity.Domain.Errors;
using Microsoft.EntityFrameworkCore;

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
    private readonly IAccountVisibilityFilter _visibilityFilter;

    public GetAccountByIdQueryHandler(
        IIdentityQueryContext context,
        IAccountVisibilityFilter visibilityFilter)
    {
        _context = context;
        _visibilityFilter = visibilityFilter;
    }

    public async Task<Result<AccountDetailDto>> Handle(
        GetAccountByIdQuery request,
        CancellationToken cancellationToken)
    {
        // Visibility check: ensure the requested account is in the visible set
        var visibleAccountIds = await _visibilityFilter.GetVisibleAccountIdsAsync(cancellationToken);
        if (visibleAccountIds != null && !visibleAccountIds.Contains(request.AccountId))
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
