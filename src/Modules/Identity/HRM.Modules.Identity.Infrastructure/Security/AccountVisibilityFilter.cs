using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Identity.Application.Abstractions.Authentication;
using HRM.Modules.Identity.Application.Abstractions.Authorization;
using HRM.Modules.Identity.Application.Abstractions.Data;
using HRM.Modules.Identity.Application.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRM.Modules.Identity.Infrastructure.Security;

/// <summary>
/// Implementation of IAccountVisibilityFilter, backed by IDataScopeService.
///
/// Used by command handlers and single-resource query handlers to check whether
/// the current user can access a specific account.
///
/// For list queries, prefer IDataScopeService directly (EF subquery — no HashSet in memory).
/// This implementation loads a HashSet, which is acceptable for single-account checks
/// where the result is used for one .Contains() call on a bounded set.
/// </summary>
public sealed class AccountVisibilityFilter : IAccountVisibilityFilter
{
    private readonly ICurrentUserService _currentUser;
    private readonly IDataScopeService _dataScopeService;
    private readonly IIdentityQueryContext _context;
    private readonly ILogger<AccountVisibilityFilter> _logger;

    public AccountVisibilityFilter(
        ICurrentUserService currentUser,
        IDataScopeService dataScopeService,
        IIdentityQueryContext context,
        ILogger<AccountVisibilityFilter> logger)
    {
        _currentUser = currentUser;
        _dataScopeService = dataScopeService;
        _context = context;
        _logger = logger;
    }

    public async Task<HashSet<Guid>?> GetVisibleAccountIdsAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser.UserId;
        var rule = await _dataScopeService.GetScopeRuleAsync(
            userId, IdentityPermissions.Account.View, cancellationToken);

        switch (rule.Level)
        {
            case DataScopeLevel.Global:
                _logger.LogDebug(
                    "System account {UserId} has unrestricted account visibility", userId);
                return null; // No filter — sees all accounts

            case DataScopeLevel.None:
                _logger.LogDebug(
                    "Account {UserId} has no account visibility (None scope)", userId);
                return []; // Explicit deny

            case DataScopeLevel.Self:
                return [userId]; // Own account only

            case DataScopeLevel.Company:
            {
                var companyIds = rule.DimensionIds;
                var accountIds = await _context.EmployeeProfiles
                    .AsNoTracking()
                    .Where(ep => ep.CompanyAccess.Any(ca => companyIds.Contains(ca.CompanyId)))
                    .Select(ep => ep.AccountId)
                    .ToListAsync(cancellationToken);

                var result = accountIds.ToHashSet();
                result.Add(userId); // Always include own account

                _logger.LogDebug(
                    "Account {UserId} ({CompanyCount} companies) can see {Count} accounts",
                    userId, companyIds.Count, result.Count);

                return result;
            }

            case DataScopeLevel.Department:
            {
                var departmentIds = rule.DimensionIds;
                var accountIds = await _context.EmployeeProfiles
                    .AsNoTracking()
                    .Where(ep => ep.DepartmentAccess.Any(da => departmentIds.Contains(da.DepartmentId)))
                    .Select(ep => ep.AccountId)
                    .ToListAsync(cancellationToken);

                var result = accountIds.ToHashSet();
                result.Add(userId);
                return result;
            }

            case DataScopeLevel.Position:
            {
                var positionIds = rule.DimensionIds;
                var accountIds = await _context.EmployeeProfiles
                    .AsNoTracking()
                    .Where(ep => ep.PositionAccess.Any(pa => positionIds.Contains(pa.PositionId)))
                    .Select(ep => ep.AccountId)
                    .ToListAsync(cancellationToken);

                var result = accountIds.ToHashSet();
                result.Add(userId);
                return result;
            }

            default:
                return [userId]; // Fallback: own account only
        }
    }
}
