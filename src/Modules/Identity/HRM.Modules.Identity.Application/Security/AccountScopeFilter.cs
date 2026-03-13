using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Identity.Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Identity.Application.Security;

/// <summary>
/// Static helper for single-account access checks based on a DataScopeRule.
///
/// For list queries, apply the DataScopeRule as an EF WHERE clause (composable subquery).
/// For single-account checks (commands, single-resource queries), use this helper
/// to verify if a specific account falls within the user's data scope.
/// </summary>
public static class AccountScopeFilter
{
    /// <summary>
    /// Check whether a specific account is accessible under the given data scope rule.
    /// </summary>
    /// <param name="rule">The resolved data scope rule for the current user.</param>
    /// <param name="currentUserId">The current user's account ID.</param>
    /// <param name="targetAccountId">The account ID being accessed.</param>
    /// <param name="context">Identity query context for dimension-based lookups.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the target account is accessible; false otherwise.</returns>
    public static async Task<bool> IsAccessibleAsync(
        DataScopeRule rule,
        Guid currentUserId,
        Guid targetAccountId,
        IIdentityQueryContext context,
        CancellationToken cancellationToken = default)
    {
        switch (rule.Level)
        {
            case DataScopeLevel.Global:
                return true;

            case DataScopeLevel.None:
                return false;

            case DataScopeLevel.Self:
                return targetAccountId == currentUserId;

            case DataScopeLevel.DirectReports:
            case DataScopeLevel.EmployeeSet:
                return targetAccountId == currentUserId
                    || await context.EmployeeProfiles
                        .AsNoTracking()
                        .Where(ep => rule.EmployeeIds.Contains(ep.EmployeeId))
                        .AnyAsync(ep => ep.AccountId == targetAccountId, cancellationToken);

            case DataScopeLevel.Company:
                return targetAccountId == currentUserId
                    || await context.EmployeeProfiles
                        .AsNoTracking()
                        .Where(ep => ep.CompanyAccess.Any(ca => rule.DimensionIds.Contains(ca.CompanyId)))
                        .AnyAsync(ep => ep.AccountId == targetAccountId, cancellationToken);

            case DataScopeLevel.Department:
                return targetAccountId == currentUserId
                    || await context.EmployeeProfiles
                        .AsNoTracking()
                        .Where(ep => ep.DepartmentAccess.Any(da => rule.DimensionIds.Contains(da.DepartmentId)))
                        .AnyAsync(ep => ep.AccountId == targetAccountId, cancellationToken);

            case DataScopeLevel.Position:
                return targetAccountId == currentUserId
                    || await context.EmployeeProfiles
                        .AsNoTracking()
                        .Where(ep => ep.PositionAccess.Any(pa => rule.DimensionIds.Contains(pa.PositionId)))
                        .AnyAsync(ep => ep.AccountId == targetAccountId, cancellationToken);

            default:
                return false;
        }
    }

    /// <summary>
    /// Apply a DataScopeRule as an EF WHERE clause to an accounts query.
    /// Used by list queries (GetAccounts) for composable subquery filtering.
    /// </summary>
    public static IQueryable<Domain.Entities.Account> ApplyScope(
        IQueryable<Domain.Entities.Account> query,
        DataScopeRule rule,
        Guid currentUserId,
        IIdentityQueryContext context)
    {
        return rule.Level switch
        {
            DataScopeLevel.Global => query,
            DataScopeLevel.None => query.Where(_ => false),
            DataScopeLevel.Self => query.Where(a => a.Id == currentUserId),
            DataScopeLevel.DirectReports or DataScopeLevel.EmployeeSet =>
                ApplyEmployeeSetScope(query, rule, context),
            DataScopeLevel.Company or DataScopeLevel.Department or DataScopeLevel.Position =>
                ApplyDimensionScope(query, rule, context),
            _ => query.Where(_ => false)
        };
    }

    private static IQueryable<Domain.Entities.Account> ApplyEmployeeSetScope(
        IQueryable<Domain.Entities.Account> query,
        DataScopeRule rule,
        IIdentityQueryContext context)
    {
        var allowedAccountIds = context.EmployeeProfiles
            .AsNoTracking()
            .Where(ep => rule.EmployeeIds.Contains(ep.EmployeeId))
            .Select(ep => ep.AccountId);

        return query.Where(a => allowedAccountIds.Contains(a.Id));
    }

    private static IQueryable<Domain.Entities.Account> ApplyDimensionScope(
        IQueryable<Domain.Entities.Account> query,
        DataScopeRule rule,
        IIdentityQueryContext context)
    {
        var allowedAccountIds = rule.Level switch
        {
            DataScopeLevel.Company => context.EmployeeProfiles
                .AsNoTracking()
                .Where(ep => ep.CompanyAccess.Any(ca => rule.DimensionIds.Contains(ca.CompanyId)))
                .Select(ep => ep.AccountId),
            DataScopeLevel.Department => context.EmployeeProfiles
                .AsNoTracking()
                .Where(ep => ep.DepartmentAccess.Any(da => rule.DimensionIds.Contains(da.DepartmentId)))
                .Select(ep => ep.AccountId),
            DataScopeLevel.Position => context.EmployeeProfiles
                .AsNoTracking()
                .Where(ep => ep.PositionAccess.Any(pa => rule.DimensionIds.Contains(pa.PositionId)))
                .Select(ep => ep.AccountId),
            _ => context.EmployeeProfiles.AsNoTracking().Where(_ => false).Select(ep => ep.AccountId)
        };

        return query.Where(a => allowedAccountIds.Contains(a.Id));
    }
}
