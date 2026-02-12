using System.Net.Http.Json;
using System.Text.Json;
using HRM.Web.Models;
using HRM.Web.Services.Abstractions;

namespace HRM.Web.Services.Identity;

/// <summary>
/// HTTP client for Identity module API endpoints.
/// Handles authentication, account management, and session management.
/// </summary>
public sealed class IdentityApiClient : IIdentityApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<IdentityApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public IdentityApiClient(
        HttpClient httpClient,
        ILogger<IdentityApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ApiResponse<AccountResponse>> RegisterAccountAsync(
        RegisterAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var apiRequest = new
            {
                request.Username,
                request.Email,
                request.Password,
                request.FullName,
                request.PhoneNumber
            };

            var response = await _httpClient.PostAsJsonAsync(
                "/api/identity/accounts/register",
                apiRequest,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<AccountResponse>(cancellationToken);
                return new ApiResponse<AccountResponse>
                {
                    IsSuccess = true,
                    Data = data
                };
            }

            return await HandleErrorResponseAsync<AccountResponse>(response, "An error occurred while processing your request", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return HandleNetworkError<AccountResponse>(ex);
        }
        catch (Exception ex)
        {
            return HandleUnexpectedError<AccountResponse>(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ApiResponse<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var apiRequest = new
            {
                request.UsernameOrEmail,
                request.Password,
                request.RememberMe
            };

            var response = await _httpClient.PostAsJsonAsync(
                "/api/identity/auth/login",
                apiRequest,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken);
                return new ApiResponse<LoginResponse>
                {
                    IsSuccess = true,
                    Data = data
                };
            }

            return await HandleErrorResponseAsync<LoginResponse>(response, "Invalid username or password", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return HandleNetworkError<LoginResponse>(ex);
        }
        catch (Exception ex)
        {
            return HandleUnexpectedError<LoginResponse>(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ApiResponse<object>> LogoutAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync(
                "/api/identity/auth/logout",
                null,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = true,
                    Data = new { message = "Logged out successfully" }
                };
            }

            return await HandleErrorResponseAsync<object>(response, "Failed to logout", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return HandleNetworkError<object>(ex);
        }
        catch (Exception ex)
        {
            return HandleUnexpectedError<object>(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ApiResponse<PagedResult<AccountSummary>>> GetAccountsAsync(
        string? searchTerm = null,
        string? status = null,
        Guid? companyId = null,
        bool allCompanies = false,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = new List<string>();
            if (!string.IsNullOrWhiteSpace(searchTerm))
                queryParams.Add($"searchTerm={Uri.EscapeDataString(searchTerm)}");
            if (!string.IsNullOrWhiteSpace(status))
                queryParams.Add($"status={Uri.EscapeDataString(status)}");
            if (companyId.HasValue)
                queryParams.Add($"companyId={companyId.Value}");
            if (allCompanies)
                queryParams.Add("allCompanies=true");
            queryParams.Add($"pageNumber={pageNumber}");
            queryParams.Add($"pageSize={pageSize}");

            var queryString = string.Join("&", queryParams);
            var url = $"/api/identity/accounts?{queryString}";

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<PagedResult<AccountSummary>>(JsonOptions, cancellationToken);
                return new ApiResponse<PagedResult<AccountSummary>>
                {
                    IsSuccess = true,
                    Data = data ?? new PagedResult<AccountSummary>()
                };
            }

            return await HandleErrorResponseAsync<PagedResult<AccountSummary>>(response, "Failed to retrieve accounts", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return HandleNetworkError<PagedResult<AccountSummary>>(ex);
        }
        catch (Exception ex)
        {
            return HandleUnexpectedError<PagedResult<AccountSummary>>(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ApiResponse<AccountResponse>> ActivateAccountAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"/api/identity/accounts/{accountId}/activate",
                null,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<AccountResponse>(cancellationToken);
                return new ApiResponse<AccountResponse>
                {
                    IsSuccess = true,
                    Data = data
                };
            }

            return await HandleErrorResponseAsync<AccountResponse>(response, "Failed to activate account", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return HandleNetworkError<AccountResponse>(ex);
        }
        catch (Exception ex)
        {
            return HandleUnexpectedError<AccountResponse>(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ApiResponse<List<SessionInfo>>> GetActiveSessionsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                "/api/identity/auth/sessions",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<List<SessionInfo>>(JsonOptions, cancellationToken);
                return new ApiResponse<List<SessionInfo>>
                {
                    IsSuccess = true,
                    Data = data ?? new List<SessionInfo>()
                };
            }

            return await HandleErrorResponseAsync<List<SessionInfo>>(response, "Failed to retrieve sessions", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return HandleNetworkError<List<SessionInfo>>(ex);
        }
        catch (Exception ex)
        {
            return HandleUnexpectedError<List<SessionInfo>>(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ApiResponse<object>> RevokeSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(
                $"/api/identity/auth/sessions/{sessionId}",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = true,
                    Data = new { message = "Session revoked successfully" }
                };
            }

            return await HandleErrorResponseAsync<object>(response, "Failed to revoke session", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return HandleNetworkError<object>(ex);
        }
        catch (Exception ex)
        {
            return HandleUnexpectedError<object>(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ApiResponse<RevokeAllSessionsResult>> RevokeAllSessionsExceptCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync(
                "/api/identity/auth/sessions/revoke-all-except-current",
                null,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<RevokeAllSessionsResult>(JsonOptions, cancellationToken);
                return new ApiResponse<RevokeAllSessionsResult>
                {
                    IsSuccess = true,
                    Data = data
                };
            }

            return await HandleErrorResponseAsync<RevokeAllSessionsResult>(response, "Failed to revoke sessions", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return HandleNetworkError<RevokeAllSessionsResult>(ex);
        }
        catch (Exception ex)
        {
            return HandleUnexpectedError<RevokeAllSessionsResult>(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ApiResponse<LoginResponse>> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new { refreshToken };

            var response = await _httpClient.PostAsJsonAsync(
                "/api/identity/auth/refresh",
                payload,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions, cancellationToken);
                return new ApiResponse<LoginResponse>
                {
                    IsSuccess = true,
                    Data = data
                };
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Token refresh failed with status {StatusCode}: {ErrorContent}",
                (int)response.StatusCode, errorContent);

            return new ApiResponse<LoginResponse>
            {
                IsSuccess = false,
                ErrorCode = "RefreshFailed",
                ErrorMessage = "Session expired. Please login again."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return new ApiResponse<LoginResponse>
            {
                IsSuccess = false,
                ErrorCode = "RefreshError",
                ErrorMessage = "Failed to refresh session."
            };
        }
    }

    #region Role Management

    /// <inheritdoc />
    public async Task<ApiResponse<IReadOnlyList<RoleResponse>>> GetRolesAsync(
        Guid? companyId = null,
        bool allCompanies = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = new List<string>();
            if (companyId.HasValue)
                queryParams.Add($"companyId={companyId.Value}");
            if (allCompanies)
                queryParams.Add("allCompanies=true");

            var url = queryParams.Count > 0
                ? $"/api/identity/roles?{string.Join("&", queryParams)}"
                : "/api/identity/roles";

            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                // Backend returns PagedResult<RoleSummaryDto> — extract Items
                var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<RoleResponse>>(JsonOptions, cancellationToken);
                return new ApiResponse<IReadOnlyList<RoleResponse>>
                {
                    IsSuccess = true,
                    Data = pagedResult?.Items ?? []
                };
            }

            return await HandleErrorResponseAsync<IReadOnlyList<RoleResponse>>(response, "Failed to retrieve roles", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return HandleNetworkError<IReadOnlyList<RoleResponse>>(ex);
        }
        catch (Exception ex)
        {
            return HandleUnexpectedError<IReadOnlyList<RoleResponse>>(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ApiResponse<RoleDetailResponse>> GetRoleByIdAsync(
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/identity/roles/{roleId}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<RoleDetailResponse>(JsonOptions, cancellationToken);
                return new ApiResponse<RoleDetailResponse>
                {
                    IsSuccess = true,
                    Data = data
                };
            }

            return await HandleErrorResponseAsync<RoleDetailResponse>(response, "Failed to retrieve role", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return HandleNetworkError<RoleDetailResponse>(ex);
        }
        catch (Exception ex)
        {
            return HandleUnexpectedError<RoleDetailResponse>(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ApiResponse<RoleResponse>> CreateRoleAsync(
        CreateRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Map Web model (RoleType string) to API model (IsSystemRole bool + CompanyId)
            var apiRequest = new
            {
                request.Name,
                request.Description,
                IsSystemRole = request.RoleType == "System",
                request.CompanyId,
                Permissions = Array.Empty<object>()
            };

            var response = await _httpClient.PostAsJsonAsync(
                "/api/identity/roles",
                apiRequest,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<RoleResponse>(JsonOptions, cancellationToken);
                return new ApiResponse<RoleResponse>
                {
                    IsSuccess = true,
                    Data = data
                };
            }

            return await HandleErrorResponseAsync<RoleResponse>(response, "Failed to create role", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return HandleNetworkError<RoleResponse>(ex);
        }
        catch (Exception ex)
        {
            return HandleUnexpectedError<RoleResponse>(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ApiResponse<RoleResponse>> UpdateRoleAsync(
        Guid roleId,
        UpdateRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(
                $"/api/identity/roles/{roleId}",
                request,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<RoleResponse>(JsonOptions, cancellationToken);
                return new ApiResponse<RoleResponse>
                {
                    IsSuccess = true,
                    Data = data
                };
            }

            return await HandleErrorResponseAsync<RoleResponse>(response, "Failed to update role", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return HandleNetworkError<RoleResponse>(ex);
        }
        catch (Exception ex)
        {
            return HandleUnexpectedError<RoleResponse>(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ApiResponse<object>> DeleteRoleAsync(
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(
                $"/api/identity/roles/{roleId}",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = true,
                    Data = new { message = "Role deleted successfully" }
                };
            }

            return await HandleErrorResponseAsync<object>(response, "Failed to delete role", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return HandleNetworkError<object>(ex);
        }
        catch (Exception ex)
        {
            return HandleUnexpectedError<object>(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ApiResponse<RoleDetailResponse>> AssignPermissionsToRoleAsync(
        Guid roleId,
        AssignPermissionsRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"/api/identity/roles/{roleId}/permissions",
                request,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<RoleDetailResponse>(JsonOptions, cancellationToken);
                return new ApiResponse<RoleDetailResponse>
                {
                    IsSuccess = true,
                    Data = data
                };
            }

            return await HandleErrorResponseAsync<RoleDetailResponse>(response, "Failed to assign permissions", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return HandleNetworkError<RoleDetailResponse>(ex);
        }
        catch (Exception ex)
        {
            return HandleUnexpectedError<RoleDetailResponse>(ex);
        }
    }

    #endregion

    #region Account-Role Management

    /// <inheritdoc />
    public async Task<ApiResponse<IReadOnlyList<RoleResponse>>> GetAccountRolesAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"/api/identity/accounts/{accountId}/roles",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<List<RoleResponse>>(JsonOptions, cancellationToken);
                return new ApiResponse<IReadOnlyList<RoleResponse>>
                {
                    IsSuccess = true,
                    Data = data ?? []
                };
            }

            return await HandleErrorResponseAsync<IReadOnlyList<RoleResponse>>(response, "Failed to retrieve account roles", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return HandleNetworkError<IReadOnlyList<RoleResponse>>(ex);
        }
        catch (Exception ex)
        {
            return HandleUnexpectedError<IReadOnlyList<RoleResponse>>(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ApiResponse<AccountResponse>> AssignRolesToAccountAsync(
        Guid accountId,
        AssignRolesToAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"/api/identity/accounts/{accountId}/roles",
                request,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<AccountResponse>(JsonOptions, cancellationToken);
                return new ApiResponse<AccountResponse>
                {
                    IsSuccess = true,
                    Data = data
                };
            }

            return await HandleErrorResponseAsync<AccountResponse>(response, "Failed to assign roles", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return HandleNetworkError<AccountResponse>(ex);
        }
        catch (Exception ex)
        {
            return HandleUnexpectedError<AccountResponse>(ex);
        }
    }

    #endregion

    #region Permission Catalog

    /// <inheritdoc />
    public async Task<ApiResponse<PermissionCatalogResponse>> GetPermissionCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                "/api/identity/permissions/catalog",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<PermissionCatalogResponse>(JsonOptions, cancellationToken);
                return new ApiResponse<PermissionCatalogResponse>
                {
                    IsSuccess = true,
                    Data = data ?? new PermissionCatalogResponse()
                };
            }

            return await HandleErrorResponseAsync<PermissionCatalogResponse>(response, "Failed to retrieve permission catalog", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return HandleNetworkError<PermissionCatalogResponse>(ex);
        }
        catch (Exception ex)
        {
            return HandleUnexpectedError<PermissionCatalogResponse>(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ApiResponse<UserPermissionsResponse>> GetMyPermissionsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                "/api/identity/permissions/me",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<UserPermissionsResponse>(JsonOptions, cancellationToken);
                return new ApiResponse<UserPermissionsResponse>
                {
                    IsSuccess = true,
                    Data = data
                };
            }

            return await HandleErrorResponseAsync<UserPermissionsResponse>(response, "Failed to retrieve permissions", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return HandleNetworkError<UserPermissionsResponse>(ex);
        }
        catch (Exception ex)
        {
            return HandleUnexpectedError<UserPermissionsResponse>(ex);
        }
    }

    #endregion

    #region Account Detail Management

    /// <inheritdoc />
    public async Task<ApiResponse<AccountDetailResponse>> GetAccountByIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"/api/identity/accounts/{accountId}",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<AccountDetailResponse>(JsonOptions, cancellationToken);
                return new ApiResponse<AccountDetailResponse>
                {
                    IsSuccess = true,
                    Data = data
                };
            }

            return await HandleErrorResponseAsync<AccountDetailResponse>(response, "Failed to retrieve account", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return HandleNetworkError<AccountDetailResponse>(ex);
        }
        catch (Exception ex)
        {
            return HandleUnexpectedError<AccountDetailResponse>(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ApiResponse<object>> SuspendAccountAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"/api/identity/accounts/{accountId}/suspend",
                null,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = true,
                    Data = new { }
                };
            }

            return await HandleErrorResponseAsync<object>(response, "Failed to suspend account", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return HandleNetworkError<object>(ex);
        }
        catch (Exception ex)
        {
            return HandleUnexpectedError<object>(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ApiResponse<object>> DeactivateAccountAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"/api/identity/accounts/{accountId}/deactivate",
                null,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = true,
                    Data = new { }
                };
            }

            return await HandleErrorResponseAsync<object>(response, "Failed to deactivate account", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return HandleNetworkError<object>(ex);
        }
        catch (Exception ex)
        {
            return HandleUnexpectedError<object>(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ApiResponse<object>> UnlockAccountAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"/api/identity/accounts/{accountId}/unlock",
                null,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = true,
                    Data = new { }
                };
            }

            return await HandleErrorResponseAsync<object>(response, "Failed to unlock account", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return HandleNetworkError<object>(ex);
        }
        catch (Exception ex)
        {
            return HandleUnexpectedError<object>(ex);
        }
    }

    #endregion

    #region Two-Factor Authentication

    /// <inheritdoc />
    public async Task<ApiResponse<EnableTwoFactorResponse>> EnableTwoFactorAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"/api/identity/accounts/{accountId}/enable-2fa",
                null,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<EnableTwoFactorResponse>(JsonOptions, cancellationToken);
                return new ApiResponse<EnableTwoFactorResponse>
                {
                    IsSuccess = true,
                    Data = data
                };
            }

            return await HandleErrorResponseAsync<EnableTwoFactorResponse>(response, "Failed to enable two-factor authentication", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return HandleNetworkError<EnableTwoFactorResponse>(ex);
        }
        catch (Exception ex)
        {
            return HandleUnexpectedError<EnableTwoFactorResponse>(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ApiResponse<object>> DisableTwoFactorAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"/api/identity/accounts/{accountId}/disable-2fa",
                null,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = true,
                    Data = new { }
                };
            }

            return await HandleErrorResponseAsync<object>(response, "Failed to disable two-factor authentication", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return HandleNetworkError<object>(ex);
        }
        catch (Exception ex)
        {
            return HandleUnexpectedError<object>(ex);
        }
    }

    #endregion

    #region Account Security

    /// <inheritdoc />
    public async Task<ApiResponse<object>> ChangePasswordAsync(
        Guid accountId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var apiRequest = new
            {
                request.CurrentPassword,
                request.NewPassword,
                request.IsAdminReset
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"/api/identity/accounts/{accountId}/change-password",
                apiRequest,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = true,
                    Data = new { }
                };
            }

            return await HandleErrorResponseAsync<object>(response, "Failed to change password", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return HandleNetworkError<object>(ex);
        }
        catch (Exception ex)
        {
            return HandleUnexpectedError<object>(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ApiResponse<object>> UpdateProfileAsync(
        Guid accountId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var apiRequest = new
            {
                request.FullName,
                request.PhoneNumber
            };

            var response = await _httpClient.PutAsJsonAsync(
                $"/api/identity/accounts/{accountId}/profile",
                apiRequest,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = true,
                    Data = new { }
                };
            }

            return await HandleErrorResponseAsync<object>(response, "Failed to update profile", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return HandleNetworkError<object>(ex);
        }
        catch (Exception ex)
        {
            return HandleUnexpectedError<object>(ex);
        }
    }

    #endregion

    #region System Profile Management

    /// <inheritdoc />
    public async Task<ApiResponse<SystemProfileResponse>> GetSystemProfileAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"/api/identity/accounts/{accountId}/system-profile",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<SystemProfileResponse>(JsonOptions, cancellationToken);
                return new ApiResponse<SystemProfileResponse>
                {
                    IsSuccess = true,
                    Data = data
                };
            }

            return await HandleErrorResponseAsync<SystemProfileResponse>(response, "Failed to retrieve system profile", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return HandleNetworkError<SystemProfileResponse>(ex);
        }
        catch (Exception ex)
        {
            return HandleUnexpectedError<SystemProfileResponse>(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ApiResponse<object>> CreateSystemProfileAsync(
        Guid accountId,
        CreateSystemProfileWebRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"/api/identity/accounts/{accountId}/system-profile",
                request,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = true,
                    Data = new { }
                };
            }

            return await HandleErrorResponseAsync<object>(response, "Failed to create system profile", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return HandleNetworkError<object>(ex);
        }
        catch (Exception ex)
        {
            return HandleUnexpectedError<object>(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ApiResponse<object>> UpdateSystemProfileAsync(
        Guid accountId,
        UpdateSystemProfileWebRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(
                $"/api/identity/accounts/{accountId}/system-profile",
                request,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = true,
                    Data = new { }
                };
            }

            return await HandleErrorResponseAsync<object>(response, "Failed to update system profile", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return HandleNetworkError<object>(ex);
        }
        catch (Exception ex)
        {
            return HandleUnexpectedError<object>(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ApiResponse<object>> GrantSuperAdminAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"/api/identity/accounts/{accountId}/system-profile/grant-super-admin",
                null,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = true,
                    Data = new { }
                };
            }

            return await HandleErrorResponseAsync<object>(response, "Failed to grant super admin", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return HandleNetworkError<object>(ex);
        }
        catch (Exception ex)
        {
            return HandleUnexpectedError<object>(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ApiResponse<object>> RevokeSuperAdminAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync(
                $"/api/identity/accounts/{accountId}/system-profile/revoke-super-admin",
                null,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = true,
                    Data = new { }
                };
            }

            return await HandleErrorResponseAsync<object>(response, "Failed to revoke super admin", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return HandleNetworkError<object>(ex);
        }
        catch (Exception ex)
        {
            return HandleUnexpectedError<object>(ex);
        }
    }

    #endregion

    #region Employee Profile Management

    /// <inheritdoc />
    public async Task<ApiResponse<EmployeeProfileResponse>> GetEmployeeProfileAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"/api/identity/accounts/{accountId}/employee-profile",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<EmployeeProfileResponse>(JsonOptions, cancellationToken);
                return new ApiResponse<EmployeeProfileResponse>
                {
                    IsSuccess = true,
                    Data = data
                };
            }

            return await HandleErrorResponseAsync<EmployeeProfileResponse>(response, "Failed to retrieve employee profile", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return HandleNetworkError<EmployeeProfileResponse>(ex);
        }
        catch (Exception ex)
        {
            return HandleUnexpectedError<EmployeeProfileResponse>(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ApiResponse<object>> CreateEmployeeProfileAsync(
        Guid accountId,
        CreateEmployeeProfileWebRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"/api/identity/accounts/{accountId}/employee-profile",
                request,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = true,
                    Data = new { }
                };
            }

            return await HandleErrorResponseAsync<object>(response, "Failed to create employee profile", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return HandleNetworkError<object>(ex);
        }
        catch (Exception ex)
        {
            return HandleUnexpectedError<object>(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ApiResponse<object>> UpdateEmployeeProfileAsync(
        Guid accountId,
        UpdateEmployeeProfileWebRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(
                $"/api/identity/accounts/{accountId}/employee-profile",
                request,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return new ApiResponse<object>
                {
                    IsSuccess = true,
                    Data = new { }
                };
            }

            return await HandleErrorResponseAsync<object>(response, "Failed to update employee profile", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return HandleNetworkError<object>(ex);
        }
        catch (Exception ex)
        {
            return HandleUnexpectedError<object>(ex);
        }
    }

    #endregion

    #region Private Helpers

    private async Task<ApiResponse<T>> HandleErrorResponseAsync<T>(
        HttpResponseMessage response,
        string defaultMessage,
        CancellationToken cancellationToken)
    {
        var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);

        try
        {
            var apiError = JsonSerializer.Deserialize<ApiErrorResponse>(errorContent, JsonOptions);
            return new ApiResponse<T>
            {
                IsSuccess = false,
                ErrorCode = apiError?.GetErrorCode() ?? "ApiError",
                ErrorMessage = apiError?.GetErrorMessage() ?? defaultMessage,
                ValidationErrors = apiError?.GetValidationErrors()
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize error response: {ErrorContent}", errorContent);
            return new ApiResponse<T>
            {
                IsSuccess = false,
                ErrorCode = "ApiError",
                ErrorMessage = $"Server returned {(int)response.StatusCode}: {errorContent}"
            };
        }
    }

    private ApiResponse<T> HandleNetworkError<T>(HttpRequestException ex)
    {
        _logger.LogError(ex, "Network error while calling Identity API");
        return new ApiResponse<T>
        {
            IsSuccess = false,
            ErrorCode = "NetworkError",
            ErrorMessage = "Failed to connect to API server. Please try again later."
        };
    }

    private ApiResponse<T> HandleUnexpectedError<T>(Exception ex)
    {
        _logger.LogError(ex, "Unexpected error while calling Identity API");
        return new ApiResponse<T>
        {
            IsSuccess = false,
            ErrorCode = "UnexpectedError",
            ErrorMessage = "An unexpected error occurred. Please contact support."
        };
    }

    #endregion
}
