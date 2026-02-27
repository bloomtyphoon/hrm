using HRM.BuildingBlocks.Application.Abstractions.Caching;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Identity.Application.Abstractions.Authorization;
using HRM.Modules.Identity.Application.Abstractions.Data;
using HRM.Modules.Identity.Infrastructure.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using DataScopeContext = HRM.Modules.Identity.Application.Abstractions.Authorization.DataScopeContext;

namespace HRM.Modules.Identity.Infrastructure.Security;

/// <summary>
/// Implementation of IDataScopeRuleProvider.
/// SINGLE SOURCE OF TRUTH for all data scoping logic.
/// Uses only Identity schema data — no cross-module queries.
///
/// Data sources (all in Identity schema):
/// - Company scope: EmployeeProfile.CompanyAccess (denormalized from Personnel)
/// - Department scope: EmployeeProfile.PrimaryDepartmentId
/// - Position scope: EmployeeProfile.PrimaryPositionId
///
/// Note: Department/Position currently use primary values only.
/// For multi-department/position support, add DepartmentAccess/PositionAccess
/// collections following the same pattern as CompanyAccess.
/// </summary>
public sealed class DataScopeRuleProvider : IDataScopeRuleProvider
{
    private readonly IIdentityQueryContext _context;
    private readonly ICache _cache;
    private readonly ILogger<DataScopeRuleProvider> _logger;
    private readonly TimeSpan _cacheDuration;

    private DataScopeRule? _cachedRule;
    private DataScopeContext? _cachedContext;

    public DataScopeRuleProvider(
        IIdentityQueryContext context,
        ICache cache,
        ILogger<DataScopeRuleProvider> logger,
        IOptions<IdentityCacheSettings> cacheSettings)
    {
        _context = context;
        _cache = cache;
        _logger = logger;
        _cacheDuration = TimeSpan.FromMinutes(cacheSettings.Value.DataScopeRuleCacheDurationMinutes);
    }

    /// <inheritdoc />
    public async Task<DataScopeRule> GetRuleAsync(
        DataScopeContext context,
        CancellationToken cancellationToken = default)
    {
        if (_cachedRule != null && ContextMatches(context))
        {
            return _cachedRule;
        }

        var rule = await BuildRuleAsync(context, cancellationToken);
        _cachedRule = rule;
        _cachedContext = context;

        return rule;
    }

    /// <inheritdoc />
    public DataScopeRule GetRule(DataScopeContext context)
    {
        if (_cachedRule != null && ContextMatches(context))
        {
            return _cachedRule;
        }

        var rule = BuildRuleSync(context);
        _cachedRule = rule;
        _cachedContext = context;

        return rule;
    }

    private bool ContextMatches(DataScopeContext context)
    {
        if (_cachedContext == null) return false;

        return _cachedContext.UserId == context.UserId
            && _cachedContext.Permission == context.Permission
            && _cachedContext.ScopeLevel == context.ScopeLevel;
    }

    private async Task<DataScopeRule> BuildRuleAsync(
        DataScopeContext context,
        CancellationToken cancellationToken)
    {
        if (context.IsSystemAccount)
        {
            _logger.LogDebug(
                "System account {UserId} granted Global access for {Permission}",
                context.UserId, context.Permission);

            return DataScopeRule.Global();
        }

        return context.ScopeLevel switch
        {
            DataScopeLevel.Global => DataScopeRule.Global(),
            DataScopeLevel.Company => await BuildCompanyScopeRuleAsync(context, cancellationToken),
            DataScopeLevel.Department => await BuildDepartmentScopeRuleAsync(context, cancellationToken),
            DataScopeLevel.Position => await BuildPositionScopeRuleAsync(context, cancellationToken),
            DataScopeLevel.EmployeeSet => throw new NotSupportedException(
                "EmployeeSet scope must be resolved by IHierarchyScopeResolver in Organization module"),
            DataScopeLevel.Self => BuildSelfScopeRule(context),
            DataScopeLevel.None => DataScopeRule.None(),
            _ => DataScopeRule.None()
        };
    }

    private DataScopeRule BuildRuleSync(DataScopeContext context)
    {
        if (context.IsSystemAccount)
        {
            return DataScopeRule.Global();
        }

        return context.ScopeLevel switch
        {
            DataScopeLevel.Global => DataScopeRule.Global(),
            DataScopeLevel.Self => BuildSelfScopeRule(context),
            DataScopeLevel.None => DataScopeRule.None(),
            _ => DataScopeRule.None()
        };
    }

    private async Task<DataScopeRule> BuildCompanyScopeRuleAsync(
        DataScopeContext context,
        CancellationToken cancellationToken)
    {
        if (!context.EmployeeId.HasValue)
        {
            _logger.LogWarning(
                "Company scope requested but no EmployeeId for user {UserId}",
                context.UserId);
            return DataScopeRule.None();
        }

        var profile = await LoadEmployeeProfileAsync(context.EmployeeId.Value, cancellationToken);
        if (profile == null)
        {
            _logger.LogWarning(
                "No EmployeeProfile found for employee {EmployeeId}",
                context.EmployeeId.Value);
            return DataScopeRule.None();
        }

        if (profile.CompanyIds.Count == 0)
        {
            _logger.LogWarning(
                "No company access found for employee {EmployeeId}",
                context.EmployeeId.Value);
            return DataScopeRule.None();
        }

        _logger.LogDebug(
            "Company scope for user {UserId}: {CompanyCount} companies",
            context.UserId, profile.CompanyIds.Count);

        return DataScopeRule.Company(profile.CompanyIds);
    }

    private async Task<DataScopeRule> BuildDepartmentScopeRuleAsync(
        DataScopeContext context,
        CancellationToken cancellationToken)
    {
        if (!context.EmployeeId.HasValue)
        {
            return DataScopeRule.None();
        }

        var profile = await LoadEmployeeProfileAsync(context.EmployeeId.Value, cancellationToken);
        if (profile?.PrimaryDepartmentId == null)
        {
            return DataScopeRule.None();
        }

        _logger.LogDebug(
            "Department scope for user {UserId}: department {DepartmentId}",
            context.UserId, profile.PrimaryDepartmentId);

        return DataScopeRule.Department([profile.PrimaryDepartmentId.Value]);
    }

    private async Task<DataScopeRule> BuildPositionScopeRuleAsync(
        DataScopeContext context,
        CancellationToken cancellationToken)
    {
        if (!context.EmployeeId.HasValue)
        {
            return DataScopeRule.None();
        }

        var profile = await LoadEmployeeProfileAsync(context.EmployeeId.Value, cancellationToken);
        if (profile?.PrimaryPositionId == null)
        {
            return DataScopeRule.None();
        }

        _logger.LogDebug(
            "Position scope for user {UserId}: position {PositionId}",
            context.UserId, profile.PrimaryPositionId);

        return DataScopeRule.Position([profile.PrimaryPositionId.Value]);
    }

    private DataScopeRule BuildSelfScopeRule(DataScopeContext context)
    {
        _logger.LogDebug(
            "Self scope for user {UserId}, employeeId {EmployeeId}",
            context.UserId, context.EmployeeId);

        if (!context.EmployeeId.HasValue)
        {
            _logger.LogWarning(
                "Self scope requested but no EmployeeId for user {UserId}",
                context.UserId);
            return DataScopeRule.None();
        }

        return DataScopeRule.Self(context.EmployeeId.Value);
    }

    private async Task<EmployeeScopeData?> LoadEmployeeProfileAsync(
        Guid employeeId,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"identity:scope:employee:{employeeId}";

        var cached = await _cache.GetAsync<EmployeeScopeData>(cacheKey, cancellationToken);
        if (cached != null)
        {
            return cached;
        }

        var profile = await _context.EmployeeProfiles
            .AsNoTracking()
            .Where(ep => ep.EmployeeId == employeeId)
            .Select(ep => new EmployeeScopeData
            {
                PrimaryDepartmentId = ep.PrimaryDepartmentId,
                PrimaryPositionId = ep.PrimaryPositionId,
                CompanyIds = ep.CompanyAccess.Select(ca => ca.CompanyId).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (profile != null)
        {
            await _cache.SetAsync(cacheKey, profile, _cacheDuration, cancellationToken);
        }

        return profile;
    }

    private sealed class EmployeeScopeData
    {
        public Guid? PrimaryDepartmentId { get; init; }
        public Guid? PrimaryPositionId { get; init; }
        public List<Guid> CompanyIds { get; init; } = [];
    }
}
