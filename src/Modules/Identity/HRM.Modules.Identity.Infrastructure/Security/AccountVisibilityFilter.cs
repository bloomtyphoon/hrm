using HRM.Modules.Identity.Application.Abstractions.Authentication;
using HRM.Modules.Identity.Application.Abstractions.Authorization;
using HRM.Modules.Identity.Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRM.Modules.Identity.Infrastructure.Security;

/// <summary>
/// Implementation of IAccountVisibilityFilter.
/// Uses only Identity schema data — no cross-module queries.
///
/// Visibility Rules:
/// - System accounts: see all accounts (no filter)
/// - Employee accounts: see accounts whose EmployeeProfile shares
///   at least one company via EmployeeProfileCompanies table
/// </summary>
public sealed class AccountVisibilityFilter : IAccountVisibilityFilter
{
    private readonly ICurrentUserService _currentUser;
    private readonly IIdentityQueryContext _context;
    private readonly ILogger<AccountVisibilityFilter> _logger;

    public AccountVisibilityFilter(
        ICurrentUserService currentUser,
        IIdentityQueryContext context,
        ILogger<AccountVisibilityFilter> logger)
    {
        _currentUser = currentUser;
        _context = context;
        _logger = logger;
    }

    public async Task<HashSet<Guid>?> GetVisibleAccountIdsAsync(
        CancellationToken cancellationToken = default)
    {
        // System accounts see everything
        if (_currentUser.IsSystemAccount())
        {
            _logger.LogDebug(
                "System account {UserId} has unrestricted account visibility",
                _currentUser.UserId);
            return null;
        }

        // Get current user's company IDs from EmployeeProfileCompanies
        var userCompanyIds = await _context.EmployeeProfiles
            .AsNoTracking()
            .Where(ep => ep.AccountId == _currentUser.UserId)
            .SelectMany(ep => ep.CompanyAccess)
            .Select(ca => ca.CompanyId)
            .ToListAsync(cancellationToken);

        // No company access → can only see own account
        if (userCompanyIds.Count == 0)
        {
            _logger.LogWarning(
                "Employee account {UserId} has no company access, returning self-only visibility",
                _currentUser.UserId);
            return [_currentUser.UserId];
        }

        // Find all accounts whose EmployeeProfile shares at least one company
        var visibleAccountIds = await _context.EmployeeProfiles
            .AsNoTracking()
            .Where(ep => ep.CompanyAccess.Any(ca => userCompanyIds.Contains(ca.CompanyId)))
            .Select(ep => ep.AccountId)
            .ToListAsync(cancellationToken);

        var result = visibleAccountIds.ToHashSet();

        // Always include own account
        result.Add(_currentUser.UserId);

        _logger.LogDebug(
            "Employee account {UserId} ({CompanyCount} companies) can see {Count} accounts",
            _currentUser.UserId, userCompanyIds.Count, result.Count);

        return result;
    }
}
