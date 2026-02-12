using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.Modules.Identity.Domain.Repositories;
using HRM.Modules.Identity.Infrastructure.Configuration;
using Microsoft.Extensions.Caching.Memory;
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
/// - User permissions cached for 5 minutes
/// - Super admin status cached for 5 minutes
/// </summary>
public sealed class PermissionService : IPermissionService
{
    private readonly IAccountPermissionRepository _permissionRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<PermissionService> _logger;
    private readonly TimeSpan _cacheDuration;

    private const string PermissionCacheKeyPrefix = "UserPermissions_";
    private const string SuperAdminCacheKeyPrefix = "IsSuperAdmin_";

    public PermissionService(
        IAccountPermissionRepository permissionRepository,
        IMemoryCache cache,
        ILogger<PermissionService> logger,
        IOptions<IdentityCacheSettings> cacheSettings)
    {
        _permissionRepository = permissionRepository ?? throw new ArgumentNullException(nameof(permissionRepository));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _cacheDuration = TimeSpan.FromMinutes(cacheSettings.Value.PermissionCacheDurationMinutes);
    }

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

        var cacheKey = $"{PermissionCacheKeyPrefix}{userId}";

        if (_cache.TryGetValue<HashSet<string>>(cacheKey, out var cachedPermissions) && cachedPermissions != null)
        {
            _logger.LogDebug("Cache hit for user {UserId} permissions", userId);
            return cachedPermissions;
        }

        _logger.LogDebug("Cache miss for user {UserId} permissions, loading from database", userId);
        var permissions = await _permissionRepository.GetPermissionsAsync(accountId, cancellationToken);

        _cache.Set(cacheKey, permissions, _cacheDuration);

        _logger.LogDebug(
            "Loaded {Count} permissions for user {UserId}",
            permissions.Count,
            userId);

        return permissions;
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

        var cacheKey = $"{SuperAdminCacheKeyPrefix}{userId}";

        if (_cache.TryGetValue<bool>(cacheKey, out var cachedResult))
        {
            return cachedResult;
        }

        var isSuperAdmin = await _permissionRepository.IsSuperAdminAsync(accountId, cancellationToken);

        _cache.Set(cacheKey, isSuperAdmin, _cacheDuration);

        if (isSuperAdmin)
        {
            _logger.LogDebug("User {UserId} is super admin", userId);
        }

        return isSuperAdmin;
    }

    /// <summary>
    /// Invalidate permission cache for a user.
    /// Call this when user's roles or permissions change.
    /// </summary>
    public void InvalidateCache(string userId)
    {
        _cache.Remove($"{PermissionCacheKeyPrefix}{userId}");
        _cache.Remove($"{SuperAdminCacheKeyPrefix}{userId}");

        _logger.LogInformation("Permission cache invalidated for user {UserId}", userId);
    }
}
