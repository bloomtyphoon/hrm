using HRM.BuildingBlocks.Domain.Abstractions.Security;

namespace HRM.BuildingBlocks.Application.Abstractions.Authorization;

/// <summary>
/// Service for checking user permissions (pure Identity concern).
///
/// Design (separation of concerns):
/// - IPermissionService answers: "Does this user have this permission?" (action-based)
/// - Data scope ("what data range?") is a SEPARATE concern handled by IDataScopeService
///
/// Permission Model:
/// - Users have Roles
/// - Roles have Permissions (Module.Entity.Action)
/// - NO ScopeLevel here — scope is resolved by business module
///
/// Usage:
/// <code>
/// // Strongly-typed (preferred — no magic strings)
/// var hasPermission = await permissionService.HasPermissionAsync(
///     userId, PersonnelPermissions.Employee.View);
///
/// // String-based (used by RoutePermissionMiddleware from RouteSecurityMap.xml)
/// var hasPermission = await permissionService.HasPermissionAsync(
///     userId, "Personnel.Employee.View");
/// </code>
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Check if user has permission using a strongly-typed descriptor.
    /// Preferred overload — delegates to HasPermissionAsync(module, entity, action).
    /// </summary>
    Task<bool> HasPermissionAsync(
        string userId,
        PermissionDescriptor permission,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if user has specific permission (module.entity.action)
    /// </summary>
    Task<bool> HasPermissionAsync(
        string userId,
        string module,
        string entity,
        string action,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if user has permission by key (e.g., "Identity.Account.View").
    /// Used by RoutePermissionMiddleware when loading permissions from RouteSecurityMap.xml.
    /// </summary>
    Task<bool> HasPermissionAsync(
        string userId,
        string permissionKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all permissions for a user (set of "Module.Entity.Action" strings)
    /// </summary>
    Task<HashSet<string>> GetUserPermissionsAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if user is a super admin (bypasses all permission checks)
    /// </summary>
    Task<bool> IsSuperAdminAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
