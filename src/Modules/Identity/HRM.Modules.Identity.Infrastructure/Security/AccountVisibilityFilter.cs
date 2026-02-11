using System.Data;
using Dapper;
using HRM.Modules.Identity.Application.Abstractions.Authentication;
using HRM.Modules.Identity.Application.Abstractions.Authorization;
using Microsoft.Extensions.Logging;

namespace HRM.Modules.Identity.Infrastructure.Security;

/// <summary>
/// Implementation of IAccountVisibilityFilter.
/// Uses Dapper to query across Identity and Personnel schemas
/// (same pattern as DataScopeRuleProvider).
///
/// Visibility Rules:
/// - System accounts: see all accounts (no filter)
/// - Employee accounts: see accounts whose employees share at least one company
/// </summary>
public sealed class AccountVisibilityFilter : IAccountVisibilityFilter
{
    private readonly ICurrentUserService _currentUser;
    private readonly IDbConnection _connection;
    private readonly ILogger<AccountVisibilityFilter> _logger;

    public AccountVisibilityFilter(
        ICurrentUserService currentUser,
        IDbConnection connection,
        ILogger<AccountVisibilityFilter> logger)
    {
        _currentUser = currentUser;
        _connection = connection;
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

        // Employee accounts without EmployeeId see nothing
        if (!_currentUser.EmployeeId.HasValue)
        {
            _logger.LogWarning(
                "Employee account {UserId} has no EmployeeId, returning empty visibility",
                _currentUser.UserId);
            return [];
        }

        // Get all AccountIds visible to this employee
        // An account is visible if the account's linked employee has active assignments
        // in the same companies as the current user's active assignments.
        const string sql = """
            SELECT DISTINCT ep.AccountId
            FROM [Identity].EmployeeProfiles ep
            INNER JOIN personnel.EmployeeAssignments ea ON ea.EmployeeId = ep.EmployeeId
            WHERE ea.CompanyId IN (
                SELECT ea2.CompanyId
                FROM personnel.EmployeeAssignments ea2
                WHERE ea2.EmployeeId = @EmployeeId
                AND (ea2.EndDate IS NULL OR ea2.EndDate > GETUTCDATE())
            )
            AND (ea.EndDate IS NULL OR ea.EndDate > GETUTCDATE())
            """;

        var accountIds = (await _connection.QueryAsync<Guid>(
            sql,
            new { EmployeeId = _currentUser.EmployeeId.Value }
        )).ToHashSet();

        // Always include own account in visibility
        accountIds.Add(_currentUser.UserId);

        _logger.LogDebug(
            "Employee account {UserId} can see {Count} accounts",
            _currentUser.UserId, accountIds.Count);

        return accountIds;
    }
}
