using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.Modules.Identity.Application.Abstractions.Data;
using HRM.Modules.Identity.Domain.Errors;
using Microsoft.EntityFrameworkCore;
using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.Modules.Identity.Application.Abstractions.Authentication;
using HRM.Modules.Identity.Application.Security;

namespace HRM.Modules.Identity.Application.Queries.GetEmployeeProfile;

internal sealed class GetEmployeeProfileQueryHandler
    : IQueryHandler<GetEmployeeProfileQuery, Result<EmployeeProfileDto>>
{
    private readonly IIdentityQueryContext _context;
    private readonly IDataScopeService _dataScopeService;
    private readonly ICurrentUserService _currentUser;

    public GetEmployeeProfileQueryHandler(
        IIdentityQueryContext context,
        IDataScopeService dataScopeService,
        ICurrentUserService currentUser)
    {
        _context = context;
        _dataScopeService = dataScopeService;
        _currentUser = currentUser;
    }

    public async Task<Result<EmployeeProfileDto>> Handle(
        GetEmployeeProfileQuery request,
        CancellationToken cancellationToken)
    {
        var rule = await _dataScopeService.GetScopeRuleAsync(
            _currentUser.UserId, IdentityPermissions.Account.View, cancellationToken);
        if (!await AccountScopeFilter.IsAccessibleAsync(rule, _currentUser.UserId, request.AccountId, _context, cancellationToken))
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
