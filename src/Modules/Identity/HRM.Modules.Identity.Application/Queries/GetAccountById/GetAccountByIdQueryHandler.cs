using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Application.Abstractions.Data;
using HRM.Modules.Identity.Application.Queries.GetAccounts;
using HRM.Modules.Identity.Domain.Errors;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Identity.Application.Queries.GetAccountById;

public sealed class GetAccountByIdQueryHandler
    : IQueryHandler<GetAccountByIdQuery, Result<AccountSummaryDto>>
{
    private readonly IIdentityQueryContext _context;

    public GetAccountByIdQueryHandler(IIdentityQueryContext context)
    {
        _context = context;
    }

    public async Task<Result<AccountSummaryDto>> Handle(
        GetAccountByIdQuery request,
        CancellationToken cancellationToken)
    {
        var account = await _context.Accounts
            .AsNoTracking()
            .Where(a => a.Id == request.AccountId)
            .Select(a => new AccountSummaryDto
            {
                Id = a.Id,
                Username = a.Username,
                Email = a.Email,
                FullName = a.FullName,
                Status = a.Status,
                AccountType = a.AccountType,
                CreatedAtUtc = a.CreatedAtUtc,
                LastLoginAtUtc = a.LastLoginAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (account is null)
        {
            return Result.Failure<AccountSummaryDto>(AccountErrors.NotFound(request.AccountId));
        }

        return Result.Success(account);
    }
}
