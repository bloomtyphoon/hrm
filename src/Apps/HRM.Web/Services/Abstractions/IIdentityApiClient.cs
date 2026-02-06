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
}
