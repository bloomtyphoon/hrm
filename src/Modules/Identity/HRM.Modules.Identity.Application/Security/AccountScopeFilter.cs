using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Identity.Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Identity.Application.Security;

/// <summary>
/// Static helper for single-account access checks based on a DataScopeRule.
///
/// Uses category-based dispatch for dynamic scope level support.
/// For dimension-based scopes, cross-references EmployeeProfiles.
/// </summary>
public static class AccountScopeFilter
{
    /// <summary>
    /// Check whether a specific account is accessible under the given data scope rule.
    /// </summary>
    public static async Task<bool> IsAccessibleAsync(
        DataScopeRule rule,
        Guid currentUserId,
        Guid targetAccountId,
        IIdentityQueryContext context,
        CancellationToken cancellationToken = default)
    {
        switch (rule.Level.Category)
        {
            case ScopeCategory.Global:
                return true;

            case ScopeCategory.None:
                return false;

            case ScopeCategory.Set when rule.Level == DataScopeLevel.Self:
                return targetAccountId == currentUserId;

            case ScopeCategory.Set:
                return targetAccountId == currentUserId
                    || await context.EmployeeProfiles
                        .AsNoTracking()
                        .Where(ep => rule.EmployeeIds.Contains(ep.EmployeeId))
                        .AnyAsync(ep => ep.AccountId == targetAccountId, cancellationToken);

            case ScopeCategory.Dimension:
                return targetAccountId == currentUserId
                    || await IsDimensionAccessibleAsync(rule, targetAccountId, context, cancellationToken);

            default:
                return false;
        }
    }

    /// <summary>
    /// Apply a DataScopeRule as an EF WHERE clause to an accounts query.
    /// </summary>
    public static IQueryable<Domain.Entities.Account> ApplyScope(
        IQueryable<Domain.Entities.Account> query,
        DataScopeRule rule,
        Guid currentUserId,
        IIdentityQueryContext context)
    {
        return rule.Level.Category switch
        {
            ScopeCategory.Global => query,
            ScopeCategory.None => query.Where(_ => false),
            ScopeCategory.Set when rule.Level == DataScopeLevel.Self =>
                query.Where(a => a.Id == currentUserId),
            ScopeCategory.Set =>
                ApplyEmployeeSetScope(query, rule, context),
            ScopeCategory.Dimension =>
                ApplyDimensionScope(query, rule, context),
            _ => query.Where(_ => false)
        };
    }

    private static async Task<bool> IsDimensionAccessibleAsync(
        DataScopeRule rule,
        Guid targetAccountId,
        IIdentityQueryContext context,
        CancellationToken cancellationToken)
    {
        // Use DimensionKey to determine which access collection to check
        return rule.Level.DimensionKey switch
        {
            DimensionKeys.Company => await context.EmployeeProfiles
                .AsNoTracking()
                .Where(ep => ep.CompanyAccess.Any(ca => rule.DimensionIds.Contains(ca.CompanyId)))
                .AnyAsync(ep => ep.AccountId == targetAccountId, cancellationToken),
            DimensionKeys.Department => await context.EmployeeProfiles
                .AsNoTracking()
                .Where(ep => ep.DepartmentAccess.Any(da => rule.DimensionIds.Contains(da.DepartmentId)))
                .AnyAsync(ep => ep.AccountId == targetAccountId, cancellationToken),
            DimensionKeys.Position => await context.EmployeeProfiles
                .AsNoTracking()
                .Where(ep => ep.PositionAccess.Any(pa => rule.DimensionIds.Contains(pa.PositionId)))
                .AnyAsync(ep => ep.AccountId == targetAccountId, cancellationToken),
            _ => false
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
        var allowedAccountIds = rule.Level.DimensionKey switch
        {
            DimensionKeys.Company => context.EmployeeProfiles
                .AsNoTracking()
                .Where(ep => ep.CompanyAccess.Any(ca => rule.DimensionIds.Contains(ca.CompanyId)))
                .Select(ep => ep.AccountId),
            DimensionKeys.Department => context.EmployeeProfiles
                .AsNoTracking()
                .Where(ep => ep.DepartmentAccess.Any(da => rule.DimensionIds.Contains(da.DepartmentId)))
                .Select(ep => ep.AccountId),
            DimensionKeys.Position => context.EmployeeProfiles
                .AsNoTracking()
                .Where(ep => ep.PositionAccess.Any(pa => rule.DimensionIds.Contains(pa.PositionId)))
                .Select(ep => ep.AccountId),
            _ => context.EmployeeProfiles.AsNoTracking().Where(_ => false).Select(ep => ep.AccountId)
        };

        return query.Where(a => allowedAccountIds.Contains(a.Id));
    }
}
