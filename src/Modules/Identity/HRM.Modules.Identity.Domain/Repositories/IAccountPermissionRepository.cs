namespace HRM.Modules.Identity.Domain.Repositories;

/// <summary>
/// Repository interface for querying account permissions
///
/// Design:
/// - Read-only queries for authorization
/// - Optimized for permission checking (not CRUD operations)
/// - Used by PermissionService for authorization
///
/// Data Flow:
/// Account -> AccountRoles -> Roles -> RolePermissions
/// </summary>
public interface IAccountPermissionRepository
{
    /// <summary>
    /// Get all permission keys for an account
    /// Returns set of "Module.Entity.Action" strings
    /// </summary>
    Task<HashSet<string>> GetPermissionsAsync(
        Guid accountId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if account has the "System Administrator" role
    /// System Administrator has all permissions (super admin)
    /// </summary>
    Task<bool> IsSuperAdminAsync(
        Guid accountId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if account has specific permission
    /// More efficient than loading all permissions
    /// </summary>
    Task<bool> HasPermissionAsync(
        Guid accountId,
        string module,
        string entity,
        string action,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all permissions with their scope levels for an account
    /// Returns dictionary of "Module.Entity.Action" -> Scope level
    /// Scope levels: 4=Global, 3=Company, 2=Department, 1=Self
    /// </summary>
    Task<Dictionary<string, int>> GetPermissionsWithScopesAsync(
        Guid accountId,
        CancellationToken cancellationToken = default);
}
