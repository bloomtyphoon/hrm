using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Application.Abstractions.Authorization;
using HRM.Modules.Identity.Application.Abstractions.Data;
using HRM.Modules.Identity.Domain.Errors;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Identity.Application.Queries.GetEmployeeProfile;

internal sealed class GetEmployeeProfileQueryHandler
    : IQueryHandler<GetEmployeeProfileQuery, Result<EmployeeProfileDto>>
{
    private readonly IIdentityQueryContext _context;
    private readonly IAccountVisibilityFilter _visibilityFilter;

    public GetEmployeeProfileQueryHandler(
        IIdentityQueryContext context,
        IAccountVisibilityFilter visibilityFilter)
    {
        _context = context;
        _visibilityFilter = visibilityFilter;
    }

    public async Task<Result<EmployeeProfileDto>> Handle(
        GetEmployeeProfileQuery request,
        CancellationToken cancellationToken)
    {
        // Visibility check
        var visibleAccountIds = await _visibilityFilter.GetVisibleAccountIdsAsync(cancellationToken);
        if (visibleAccountIds != null && !visibleAccountIds.Contains(request.AccountId))
        {
            return Result.Failure<EmployeeProfileDto>(AccountErrors.NotFound(request.AccountId));
        }

        var profile = await _context.EmployeeProfiles
            .AsNoTracking()
            .Where(ep => ep.AccountId == request.AccountId)
            .Select(ep => new EmployeeProfileDto
            {
                Id = ep.Id,
                AccountId = ep.AccountId,
                EmployeeId = ep.EmployeeId,
                DefaultScopeLevel = ep.DefaultScopeLevel,
                PrimaryCompanyId = ep.PrimaryCompanyId,
                PrimaryDepartmentId = ep.PrimaryDepartmentId,
                PrimaryPositionId = ep.PrimaryPositionId,
                CanAccessAllAssignedCompanies = ep.CanAccessAllAssignedCompanies,
                CreatedAtUtc = ep.CreatedAtUtc,
                ModifiedAtUtc = ep.ModifiedAtUtc
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (profile is null)
        {
            return Result.Failure<EmployeeProfileDto>(ProfileErrors.EmployeeProfileNotFound(request.AccountId));
        }

        return Result.Success(profile);
    }
}
