using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Application.Abstractions.Data;
using HRM.Modules.Identity.Domain.Errors;
using Microsoft.EntityFrameworkCore;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.Modules.Identity.Application.Abstractions.Authentication;
using HRM.Modules.Identity.Application.Security;

namespace HRM.Modules.Identity.Application.Queries.GetSystemProfile;

internal sealed class GetSystemProfileQueryHandler
    : IQueryHandler<GetSystemProfileQuery, Result<SystemProfileDto>>
{
    private readonly IIdentityQueryContext _context;
    private readonly IDataScopeService _dataScopeService;
    private readonly ICurrentUserService _currentUser;

    public GetSystemProfileQueryHandler(
        IIdentityQueryContext context,
        IDataScopeService dataScopeService,
        ICurrentUserService currentUser)
    {
        _context = context;
        _dataScopeService = dataScopeService;
        _currentUser = currentUser;
    }

    public async Task<Result<SystemProfileDto>> Handle(
        GetSystemProfileQuery request,
        CancellationToken cancellationToken)
    {
        var rule = await _dataScopeService.GetScopeRuleAsync(
            _currentUser.UserId, IdentityPermissions.Account.View, cancellationToken);
        if (!await AccountScopeFilter.IsAccessibleAsync(rule, _currentUser.UserId, request.AccountId, _context, cancellationToken))
        {
            return Result.Failure<SystemProfileDto>(AccountErrors.NotFound(request.AccountId));
        }

        var profile = await _context.SystemProfiles
            .AsNoTracking()
            .Where(sp => sp.AccountId == request.AccountId)
            .Select(sp => new SystemProfileDto
            {
                Id = sp.Id,
                AccountId = sp.AccountId,
                IsSuperAdmin = sp.IsSuperAdmin,
                Department = sp.Department,
                JobTitle = sp.JobTitle,
                Notes = sp.Notes,
                CreatedAtUtc = sp.CreatedAtUtc,
                ModifiedAtUtc = sp.ModifiedAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (profile is null)
        {
            return Result.Failure<SystemProfileDto>(ProfileErrors.SystemProfileNotFound(request.AccountId));
        }

        return Result.Success(profile);
    }
}
