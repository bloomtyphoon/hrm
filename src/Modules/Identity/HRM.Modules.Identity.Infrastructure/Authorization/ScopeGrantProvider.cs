using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Identity.Application.Abstractions.Data;
using HRM.Modules.Identity.Domain.Enums;
using HRM.Modules.Identity.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRM.Modules.Identity.Infrastructure.Authorization;

/// <summary>
/// Identity module implementation of IScopeGrantProvider.
///
/// Resolves the scope level a user has for a given permission by:
/// 1. Checking AccountType — System accounts get global access unconditionally
/// 2. Looking up permission scope via IAccountPermissionRepository (MAX scope across roles)
/// 3. Returning ScopeGrant with the employee's Personnel employee ID
///
/// This is consumed by Organization and Personnel modules via IScopeGrantProvider
/// to determine what data a user is allowed to see.
/// </summary>
public sealed class ScopeGrantProvider : IScopeGrantProvider
{
    private readonly IAccountPermissionRepository _permissionRepo;
    private readonly IIdentityQueryContext _context;
    private readonly ILogger<ScopeGrantProvider> _logger;

    public ScopeGrantProvider(
        IAccountPermissionRepository permissionRepo,
        IIdentityQueryContext context,
        ILogger<ScopeGrantProvider> logger)
    {
        _permissionRepo = permissionRepo;
        _context = context;
        _logger = logger;
    }

    public async Task<ScopeGrant> GetGrantAsync(
        Guid userId,
        PermissionDescriptor permission,
        CancellationToken cancellationToken = default)
    {
        // 1. Check AccountType — System accounts bypass permission checks entirely
        var accountType = await _context.Accounts
            .AsNoTracking()
            .Where(a => a.Id == userId)
            .Select(a => a.AccountType)
            .FirstOrDefaultAsync(cancellationToken);

        if (accountType == AccountType.System)
        {
            _logger.LogDebug(
                "System account {UserId} granted global scope for {Permission}",
                userId, permission.Name);
            return ScopeGrant.SystemAccount;
        }

        // 2. Get scope level for this permission (MAX across all assigned roles)
        var scopeMap = await _permissionRepo.GetPermissionsWithScopesAsync(userId, cancellationToken);
        if (!scopeMap.TryGetValue(permission.Name, out var scopeLevelInt))
        {
            _logger.LogDebug(
                "Account {UserId} has no grant for permission {Permission}",
                userId, permission.Name);
            return ScopeGrant.NoAccess;
        }

        // 3. Get Personnel EmployeeId from EmployeeProfile (cross-module link)
        var employeeId = await _context.EmployeeProfiles
            .AsNoTracking()
            .Where(ep => ep.AccountId == userId)
            .Select(ep => ep.EmployeeId)
            .FirstOrDefaultAsync(cancellationToken);

        if (employeeId == Guid.Empty)
        {
            _logger.LogWarning(
                "Employee account {UserId} has no EmployeeProfile, denying access for {Permission}",
                userId, permission.Name);
            return ScopeGrant.NoAccess;
        }

        var level = DataScopeLevel.FromId(scopeLevelInt);
        _logger.LogDebug(
            "Account {UserId} granted {Level} scope for {Permission} (employee {EmployeeId})",
            userId, level, permission.Name, employeeId);

        return ScopeGrant.ForEmployee(employeeId, level);
    }
}
