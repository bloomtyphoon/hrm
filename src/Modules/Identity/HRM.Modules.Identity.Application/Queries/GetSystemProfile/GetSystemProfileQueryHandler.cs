using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Application.Abstractions.Authorization;
using HRM.Modules.Identity.Application.Abstractions.Data;
using HRM.Modules.Identity.Domain.Errors;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Identity.Application.Queries.GetSystemProfile;

internal sealed class GetSystemProfileQueryHandler
    : IQueryHandler<GetSystemProfileQuery, Result<SystemProfileDto>>
{
    private readonly IIdentityQueryContext _context;
    private readonly IAccountVisibilityFilter _visibilityFilter;

    public GetSystemProfileQueryHandler(
        IIdentityQueryContext context,
        IAccountVisibilityFilter visibilityFilter)
    {
        _context = context;
        _visibilityFilter = visibilityFilter;
    }

    public async Task<Result<SystemProfileDto>> Handle(
        GetSystemProfileQuery request,
        CancellationToken cancellationToken)
    {
        // Visibility check
        var visibleAccountIds = await _visibilityFilter.GetVisibleAccountIdsAsync(cancellationToken);
        if (visibleAccountIds != null && !visibleAccountIds.Contains(request.AccountId))
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
