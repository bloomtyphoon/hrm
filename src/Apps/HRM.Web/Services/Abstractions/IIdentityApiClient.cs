using HRM.Web.Models;

namespace HRM.Web.Services.Abstractions;

/// <summary>
/// API client for Identity module operations.
/// Handles authentication, account management, and session management.
/// </summary>
public interface IIdentityApiClient
{
    /// <summary>
    /// Register a new account.
    /// </summary>
    Task<ApiResponse<AccountResponse>> RegisterAccountAsync(
        RegisterAccountRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Login with credentials.
    /// </summary>
    Task<ApiResponse<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Logout current session.
    /// </summary>
    Task<ApiResponse<object>> LogoutAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get paginated list of accounts.
    /// </summary>
    Task<ApiResponse<PagedResult<AccountSummary>>> GetAccountsAsync(
        string? searchTerm = null,
        string? status = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Activate a pending account.
    /// </summary>
    Task<ApiResponse<AccountResponse>> ActivateAccountAsync(
        Guid accountId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get active sessions for current user.
    /// </summary>
    Task<ApiResponse<List<SessionInfo>>> GetActiveSessionsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke a specific session.
    /// </summary>
    Task<ApiResponse<object>> RevokeSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revoke all sessions except current.
    /// </summary>
    Task<ApiResponse<RevokeAllSessionsResult>> RevokeAllSessionsExceptCurrentAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refresh access token using refresh token.
    /// </summary>
    Task<ApiResponse<LoginResponse>> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    #region Role Management

    /// <summary>
    /// Get all roles.
    /// </summary>
    Task<ApiResponse<IReadOnlyList<RoleResponse>>> GetRolesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get role by ID with permissions.
    /// </summary>
    Task<ApiResponse<RoleDetailResponse>> GetRoleByIdAsync(
        Guid roleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new role.
    /// </summary>
    Task<ApiResponse<RoleResponse>> CreateRoleAsync(
        CreateRoleRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing role.
    /// </summary>
    Task<ApiResponse<RoleResponse>> UpdateRoleAsync(
        Guid roleId,
        UpdateRoleRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a role.
    /// </summary>
    Task<ApiResponse<object>> DeleteRoleAsync(
        Guid roleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Assign permissions to a role.
    /// </summary>
    Task<ApiResponse<RoleDetailResponse>> AssignPermissionsToRoleAsync(
        Guid roleId,
        AssignPermissionsRequest request,
        CancellationToken cancellationToken = default);

    #endregion

    #region Account-Role Management

    /// <summary>
    /// Get roles assigned to an account.
    /// </summary>
    Task<ApiResponse<IReadOnlyList<RoleResponse>>> GetAccountRolesAsync(
        Guid accountId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Assign roles to an account.
    /// </summary>
    Task<ApiResponse<AccountResponse>> AssignRolesToAccountAsync(
        Guid accountId,
        AssignRolesToAccountRequest request,
        CancellationToken cancellationToken = default);

    #endregion

    #region Permission Catalog

    /// <summary>
    /// Get the permission catalog (all available permissions).
    /// </summary>
    Task<ApiResponse<PermissionCatalogResponse>> GetPermissionCatalogAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current user's effective permissions.
    /// </summary>
    Task<ApiResponse<UserPermissionsResponse>> GetMyPermissionsAsync(
        CancellationToken cancellationToken = default);

    #endregion
}
