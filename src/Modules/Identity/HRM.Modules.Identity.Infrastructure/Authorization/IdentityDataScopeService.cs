using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Identity.Application.Abstractions.Authorization;

namespace HRM.Modules.Identity.Infrastructure.Authorization;

/// <summary>
/// Identity module implementation of IDataScopeService.
///
/// Delegates to the existing IDataScopeRuleProvider (single source of truth for scope rules)
/// after resolving the user's scope grant via IScopeGrantProvider.
///
/// Flow:
///   GetScopeRuleAsync(userId, permission)
///     → IScopeGrantProvider.GetGrantAsync()        [Identity: what scope level does this user have?]
///     → IDataScopeRuleProvider.GetRuleAsync()      [Identity: what dimensions match that level?]
///     → DataScopeRule                              [returned to handler for EF filtering]
///
/// No logic is duplicated — DataScopeRuleProvider already handles all scope resolution.
/// </summary>
public sealed class IdentityDataScopeService : IDataScopeService
{
    private readonly IScopeGrantProvider _grantProvider;
    private readonly IDataScopeRuleProvider _ruleProvider;

    public IdentityDataScopeService(
        IScopeGrantProvider grantProvider,
        IDataScopeRuleProvider ruleProvider)
    {
        _grantProvider = grantProvider;
        _ruleProvider = ruleProvider;
    }

    public async Task<DataScopeRule> GetScopeRuleAsync(
        Guid userId,
        PermissionDescriptor permission,
        CancellationToken cancellationToken = default)
    {
        var grant = await _grantProvider.GetGrantAsync(userId, permission, cancellationToken);

        var context = new DataScopeContext
        {
            UserId          = userId,
            ScopeLevel      = grant.Level,
            Permission      = permission.Name,
            IsSystemAccount = grant.IsSystemAccount,
            EmployeeId      = grant.EmployeeId
        };

        return await _ruleProvider.GetRuleAsync(context, cancellationToken);
    }
}
