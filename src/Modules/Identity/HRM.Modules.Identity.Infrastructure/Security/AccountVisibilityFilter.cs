using HRM.Modules.Identity.Application.Abstractions.Authentication;
using HRM.Modules.Identity.Application.Abstractions.Authorization;
using HRM.Modules.Identity.Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRM.Modules.Identity.Infrastructure.Security;

/// <summary>
/// Implementation of IAccountVisibilityFilter.
/// Uses only Identity schema data (EmployeeProfile.PrimaryCompanyId) — no cross-module queries.
///
/// Visibility Rules:
/// - System accounts: see all accounts (no filter)
/// - Employee accounts: see accounts whose EmployeeProfile.PrimaryCompanyId matches
///   one of the current user's company IDs
///
/// Note: Currently uses PrimaryCompanyId for matching.
/// For full multi-company support, consider syncing company assignments
/// into Identity schema via integration events from Personnel module.
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

        // Get current user's EmployeeProfile to find their PrimaryCompanyId
        var currentProfile = await _context.EmployeeProfiles
            .AsNoTracking()
            .Where(ep => ep.AccountId == _currentUser.UserId)
            .Select(ep => new { ep.PrimaryCompanyId })
            .FirstOrDefaultAsync(cancellationToken);

        // No EmployeeProfile or no PrimaryCompanyId → can only see own account
        if (currentProfile?.PrimaryCompanyId == null)
        {
            _logger.LogWarning(
                "Employee account {UserId} has no PrimaryCompanyId, returning self-only visibility",
                _currentUser.UserId);
            return [_currentUser.UserId];
        }

        var companyId = currentProfile.PrimaryCompanyId.Value;

        // Find all accounts whose EmployeeProfile has the same PrimaryCompanyId
        var visibleAccountIds = await _context.EmployeeProfiles
            .AsNoTracking()
            .Where(ep => ep.PrimaryCompanyId == companyId)
            .Select(ep => ep.AccountId)
            .ToListAsync(cancellationToken);

        var result = visibleAccountIds.ToHashSet();

        // Always include own account
        result.Add(_currentUser.UserId);

        _logger.LogDebug(
            "Employee account {UserId} (company {CompanyId}) can see {Count} accounts",
            _currentUser.UserId, companyId, result.Count);

        return result;
    }
}
