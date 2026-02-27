using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Caching;
using HRM.BuildingBlocks.Application.Abstractions.Multitenancy;
using HRM.BuildingBlocks.Domain.Abstractions.Security;
using HRM.Modules.Identity.Domain.Repositories;
using HRM.Modules.Identity.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HRM.Modules.Identity.Infrastructure.Services;

/// <summary>
/// Implementation of IPermissionService (pure Identity concern).
///
/// Design:
/// - ONLY answers "does this user have this permission?" (action-based)
/// - Does NOT know about ScopeLevel, Company, Department (data scope)
/// - Data scope is a separate concern handled by IDataScopeService (business module)
///
/// Data Flow:
/// Account -> AccountRoles -> Roles -> RolePermissions -> Permission key
///
/// Caching:
/// - User permissions cached for 5 minutes per tenant
/// - Super admin status cached for 5 minutes per tenant
/// - Cache key format: identity:{tenantId}:permissions:{userId}
/// </summary>
public sealed class PermissionService : IPermissionService
{
    private readonly IAccountPermissionRepository _permissionRepository;
    private readonly ICache _cache;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<PermissionService> _logger;
    private readonly TimeSpan _cacheDuration;

    public PermissionService(
        IAccountPermissionRepository permissionRepository,
        ICache cache,
        ITenantContext tenantContext,
        ILogger<PermissionService> logger,
        IOptions<IdentityCacheSettings> cacheSettings)
    {
        _permissionRepository = permissionRepository ?? throw new ArgumentNullException(nameof(permissionRepository));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _tenantContext = tenantContext ?? throw new ArgumentNullException(nameof(tenantContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheDuration = TimeSpan.FromMinutes(cacheSettings.Value.PermissionCacheDurationMinutes);
    }

    /// <inheritdoc />
    public Task<bool> HasPermissionAsync(
        string userId,
        PermissionDescriptor permission,
        CancellationToken cancellationToken = default)
        => HasPermissionAsync(userId, permission.Module, permission.Entity, permission.Action, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> HasPermissionAsync(
        string userId,
        string module,
        string entity,
        string action,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out _))
        {
            return false;
        }

        // Super admin bypasses all permission checks
        if (await IsSuperAdminAsync(userId, cancellationToken))
        {
            _logger.LogDebug(
                "Super admin bypass for user {UserId}, permission {Module}.{Entity}.{Action}",
                userId, module, entity, action);
            return true;
        }

        var permissions = await GetUserPermissionsAsync(userId, cancellationToken);
        var permissionKey = $"{module}.{entity}.{action}";

        var hasPermission = permissions.Contains(permissionKey);

        _logger.LogDebug(
            "Permission check for user {UserId}: {Module}.{Entity}.{Action} = {Result}",
            userId, module, entity, action, hasPermission);

        return hasPermission;
    }

    /// <inheritdoc />
    public async Task<bool> HasPermissionAsync(
        string userId,
        string permissionKey,
        CancellationToken cancellationToken = default)
    {
        var parts = permissionKey.Split('.');
        if (parts.Length != 3)
        {
            _logger.LogWarning("Invalid permission key format: {PermissionKey}", permissionKey);
            return false;
        }

        return await HasPermissionAsync(userId, parts[0], parts[1], parts[2], cancellationToken);
    }

    /// <inheritdoc />
    public async Task<HashSet<string>> GetUserPermissionsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var accountId))
        {
            return [];
        }

        var cacheKey = BuildPermissionCacheKey(userId);

        return await _cache.GetOrCreateAsync(
            cacheKey,
            async () =>
            {
                _logger.LogDebug("Cache miss for user {UserId} permissions, loading from database", userId);
                var permissions = await _permissionRepository.GetPermissionsAsync(accountId, cancellationToken);
                _logger.LogDebug("Loaded {Count} permissions for user {UserId}", permissions.Count, userId);
                return permissions;
            },
            _cacheDuration,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> IsSuperAdminAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var accountId))
        {
            return false;
        }

        var cacheKey = BuildSuperAdminCacheKey(userId);

        var isSuperAdmin = await _cache.GetOrCreateAsync(
            cacheKey,
            async () =>
            {
                var result = await _permissionRepository.IsSuperAdminAsync(accountId, cancellationToken);
                if (result)
                {
                    _logger.LogDebug("User {UserId} is super admin", userId);
                }
                return result;
            },
            _cacheDuration,
            cancellationToken);

        return isSuperAdmin;
    }

    /// <summary>
    /// Invalidate permission cache for a user.
    /// Call this when user's roles or permissions change.
    /// </summary>
    public async Task InvalidateCacheAsync(string userId, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(BuildPermissionCacheKey(userId), cancellationToken);
        await _cache.RemoveAsync(BuildSuperAdminCacheKey(userId), cancellationToken);

        _logger.LogInformation("Permission cache invalidated for user {UserId}", userId);
    }

    private string BuildPermissionCacheKey(string userId)
    {
        var tenantId = _tenantContext.TenantId?.ToString() ?? "none";
        return $"identity:{tenantId}:permissions:{userId}";
    }

    private string BuildSuperAdminCacheKey(string userId)
    {
        var tenantId = _tenantContext.TenantId?.ToString() ?? "none";
        return $"identity:{tenantId}:superadmin:{userId}";
    }
}
