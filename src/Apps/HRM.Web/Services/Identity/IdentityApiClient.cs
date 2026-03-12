using System.Net.Http.Json;
using HRM.Web.Models;
using HRM.Web.Services.Abstractions;

namespace HRM.Web.Services.Identity;

/// <summary>
/// HTTP client for Identity module API endpoints.
/// Handles authentication, account management, and session management.
/// </summary>
public sealed class IdentityApiClient(
    HttpClient httpClient,
    ILogger<IdentityApiClient> logger)
    : ApiClientBase(httpClient, logger), IIdentityApiClient
{
    /// <inheritdoc />
    public Task<ApiResponse<AccountResponse>> RegisterAccountAsync(
        RegisterAccountRequest request,
        CancellationToken cancellationToken = default)
        => PostAsync<AccountResponse>("/api/identity/accounts/register",
            new
            {
                request.Username,
                request.Email,
                request.Password,
                request.FullName,
                request.AccountType,
                request.PhoneNumber
            },
            "An error occurred while processing your request", cancellationToken);

    /// <inheritdoc />
    public Task<ApiResponse<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
        => PostAsync<LoginResponse>("/api/identity/auth/login",
            new { request.UsernameOrEmail, request.Password, request.RememberMe },
            "Invalid username or password", cancellationToken);

    /// <inheritdoc />
    public Task<ApiResponse<object>> LogoutAsync(
        CancellationToken cancellationToken = default)
        => PostAsync<object>("/api/identity/auth/logout", "Failed to logout", cancellationToken);

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

        var response = await HttpClient.GetAsync(
            $"/api/identity/accounts?{string.Join("&", queryParams)}", cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<PagedResult<AccountSummary>>(JsonOptions, cancellationToken);
            return new ApiResponse<PagedResult<AccountSummary>> { IsSuccess = true, Data = data ?? new PagedResult<AccountSummary>() };
        }
        return await HandleErrorResponseAsync<PagedResult<AccountSummary>>(response, "Failed to retrieve accounts", cancellationToken);
    }

    /// <inheritdoc />
    public Task<ApiResponse<AccountResponse>> ActivateAccountAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
        => PostAsync<AccountResponse>(
            $"/api/identity/accounts/{accountId}/activate",
            "Failed to activate account", cancellationToken);

    /// <inheritdoc />
    public async Task<ApiResponse<List<SessionInfo>>> GetActiveSessionsAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await HttpClient.GetAsync("/api/identity/auth/sessions", cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<List<SessionInfo>>(JsonOptions, cancellationToken);
            return new ApiResponse<List<SessionInfo>> { IsSuccess = true, Data = data ?? [] };
        }
        return await HandleErrorResponseAsync<List<SessionInfo>>(response, "Failed to retrieve sessions", cancellationToken);
    }

    /// <inheritdoc />
    public Task<ApiResponse<object>> RevokeSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
        => DeleteAsync<object>(
            $"/api/identity/auth/sessions/{sessionId}",
            "Failed to revoke session", cancellationToken);

    /// <inheritdoc />
    public Task<ApiResponse<RevokeAllSessionsResult>> RevokeAllSessionsExceptCurrentAsync(
        CancellationToken cancellationToken = default)
        => PostAsync<RevokeAllSessionsResult>(
            "/api/identity/auth/sessions/revoke-all-except-current",
            "Failed to revoke sessions", cancellationToken);

    /// <inheritdoc />
    public async Task<ApiResponse<LoginResponse>> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await HttpClient.PostAsJsonAsync(
                "/api/identity/auth/refresh", new { refreshToken }, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions, cancellationToken);
                return new ApiResponse<LoginResponse> { IsSuccess = true, Data = data };
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("Token refresh failed with status {StatusCode}: {ErrorContent}",
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
            logger.LogError(ex, "Error during token refresh");
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
        var queryParams = new List<string>();
        if (companyId.HasValue) queryParams.Add($"companyId={companyId.Value}");
        if (allCompanies) queryParams.Add("allCompanies=true");

        var url = queryParams.Count > 0
            ? $"/api/identity/roles?{string.Join("&", queryParams)}"
            : "/api/identity/roles";

        var response = await HttpClient.GetAsync(url, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            // Backend returns PagedResult<RoleSummaryDto> — extract Items
            var pagedResult = await response.Content.ReadFromJsonAsync<PagedResult<RoleResponse>>(JsonOptions, cancellationToken);
            return new ApiResponse<IReadOnlyList<RoleResponse>> { IsSuccess = true, Data = pagedResult?.Items ?? [] };
        }
        return await HandleErrorResponseAsync<IReadOnlyList<RoleResponse>>(response, "Failed to retrieve roles", cancellationToken);
    }

    /// <inheritdoc />
    public Task<ApiResponse<RoleDetailResponse>> GetRoleByIdAsync(
        Guid roleId,
        CancellationToken cancellationToken = default)
        => GetAsync<RoleDetailResponse>($"/api/identity/roles/{roleId}", "Failed to retrieve role", cancellationToken);

    /// <inheritdoc />
    public Task<ApiResponse<RoleResponse>> CreateRoleAsync(
        CreateRoleRequest request,
        CancellationToken cancellationToken = default)
        => PostAsync<RoleResponse>("/api/identity/roles",
            new
            {
                request.Name,
                request.Description,
                IsSystemRole = request.RoleType == "System",
                request.CompanyId,
                Permissions = Array.Empty<object>()
            },
            "Failed to create role", cancellationToken);

    /// <inheritdoc />
    public Task<ApiResponse<RoleResponse>> UpdateRoleAsync(
        Guid roleId,
        UpdateRoleRequest request,
        CancellationToken cancellationToken = default)
        => PutAsync<RoleResponse>($"/api/identity/roles/{roleId}", request, "Failed to update role", cancellationToken);

    /// <inheritdoc />
    public Task<ApiResponse<object>> DeleteRoleAsync(
        Guid roleId,
        CancellationToken cancellationToken = default)
        => DeleteAsync<object>($"/api/identity/roles/{roleId}", "Failed to delete role", cancellationToken);

    /// <inheritdoc />
    public async Task<ApiResponse<RoleDetailResponse>> AssignPermissionsToRoleAsync(
        Guid roleId,
        AssignPermissionsRequest request,
        CancellationToken cancellationToken = default)
    {
        // Backend has no dedicated permissions endpoint.
        // Permissions are managed via PUT /api/identity/roles/{id} (UpdateRole).
        var roleResponse = await GetRoleByIdAsync(roleId, cancellationToken);
        if (!roleResponse.IsSuccess || roleResponse.Data == null)
            return new ApiResponse<RoleDetailResponse>
            {
                IsSuccess = false,
                ErrorMessage = roleResponse.ErrorMessage ?? "Failed to retrieve role for permission update"
            };

        var role = roleResponse.Data;
        var updatePayload = new
        {
            Name = role.Name,
            Description = role.Description,
            IsActive = role.IsActive,
            Permissions = request.Permissions.Select(p => new
            {
                p.Module,
                p.Entity,
                p.Action,
                p.Scope
            }).ToList()
        };

        return await PutAsync<RoleDetailResponse>($"/api/identity/roles/{roleId}", updatePayload,
            "Failed to assign permissions", cancellationToken);
    }

    #endregion

    #region Account-Role Management

    /// <inheritdoc />
    public async Task<ApiResponse<IReadOnlyList<RoleResponse>>> GetAccountRolesAsync(
        Guid accountId,
        Guid? companyId = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"/api/identity/accounts/{accountId}/roles";
        if (companyId.HasValue) url += $"?companyId={companyId.Value}";

        var response = await HttpClient.GetAsync(url, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<List<RoleResponse>>(JsonOptions, cancellationToken);
            return new ApiResponse<IReadOnlyList<RoleResponse>> { IsSuccess = true, Data = data ?? [] };
        }
        return await HandleErrorResponseAsync<IReadOnlyList<RoleResponse>>(response, "Failed to retrieve account roles", cancellationToken);
    }

    /// <inheritdoc />
    public Task<ApiResponse<AccountResponse>> AssignRolesToAccountAsync(
        Guid accountId,
        AssignRolesToAccountRequest request,
        CancellationToken cancellationToken = default)
        => PostAsync<AccountResponse>(
            $"/api/identity/accounts/{accountId}/roles", request,
            "Failed to assign roles", cancellationToken);

    /// <inheritdoc />
    public async Task<ApiResponse<object>> RemoveRolesFromAccountAsync(
        Guid accountId,
        AssignRolesToAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        var httpRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/identity/accounts/{accountId}/roles")
        {
            Content = JsonContent.Create(request, options: JsonOptions)
        };
        var response = await HttpClient.SendAsync(httpRequest, cancellationToken);
        if (response.IsSuccessStatusCode)
            return new ApiResponse<object> { IsSuccess = true };
        return await HandleErrorResponseAsync<object>(response, "Failed to remove roles", cancellationToken);
    }

    #endregion

    #region Permission Catalog

    /// <inheritdoc />
    public async Task<ApiResponse<PermissionCatalogResponse>> GetPermissionCatalogAsync(
        CancellationToken cancellationToken = default)
    {
        var response = await HttpClient.GetAsync("/api/identity/permissions/catalog", cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<PermissionCatalogResponse>(JsonOptions, cancellationToken);
            return new ApiResponse<PermissionCatalogResponse> { IsSuccess = true, Data = data ?? new PermissionCatalogResponse() };
        }
        return await HandleErrorResponseAsync<PermissionCatalogResponse>(response, "Failed to retrieve permission catalog", cancellationToken);
    }

    /// <inheritdoc />
    public Task<ApiResponse<UserPermissionsResponse>> GetMyPermissionsAsync(
        CancellationToken cancellationToken = default)
        => GetAsync<UserPermissionsResponse>("/api/identity/permissions/me", "Failed to retrieve permissions", cancellationToken);

    #endregion

    #region Account Detail Management

    /// <inheritdoc />
    public Task<ApiResponse<AccountDetailResponse>> GetAccountByIdAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
        => GetAsync<AccountDetailResponse>($"/api/identity/accounts/{accountId}", "Failed to retrieve account", cancellationToken);

    /// <inheritdoc />
    public Task<ApiResponse<object>> SuspendAccountAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
        => PostAsync<object>($"/api/identity/accounts/{accountId}/suspend", "Failed to suspend account", cancellationToken);

    /// <inheritdoc />
    public Task<ApiResponse<object>> DeactivateAccountAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
        => PostAsync<object>($"/api/identity/accounts/{accountId}/deactivate", "Failed to deactivate account", cancellationToken);

    /// <inheritdoc />
    public Task<ApiResponse<object>> UnlockAccountAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
        => PostAsync<object>($"/api/identity/accounts/{accountId}/unlock", "Failed to unlock account", cancellationToken);

    #endregion

    #region Two-Factor Authentication

    /// <inheritdoc />
    public Task<ApiResponse<EnableTwoFactorResponse>> EnableTwoFactorAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
        => PostAsync<EnableTwoFactorResponse>(
            $"/api/identity/accounts/{accountId}/enable-2fa",
            "Failed to enable two-factor authentication", cancellationToken);

    /// <inheritdoc />
    public Task<ApiResponse<object>> DisableTwoFactorAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
        => PostAsync<object>(
            $"/api/identity/accounts/{accountId}/disable-2fa",
            "Failed to disable two-factor authentication", cancellationToken);

    #endregion

    #region Account Security

    /// <inheritdoc />
    public Task<ApiResponse<object>> ChangeMyPasswordAsync(
        Guid accountId,
        ChangeMyPasswordRequest request,
        CancellationToken cancellationToken = default)
        => PostAsync<object>(
            $"/api/identity/accounts/{accountId}/change-my-password",
            new { request.CurrentPassword, request.NewPassword },
            "Failed to change password", cancellationToken);

    /// <inheritdoc />
    public Task<ApiResponse<object>> ResetAccountPasswordAsync(
        Guid accountId,
        ResetAccountPasswordRequest request,
        CancellationToken cancellationToken = default)
        => PostAsync<object>(
            $"/api/identity/accounts/{accountId}/reset-password",
            new { request.NewPassword },
            "Failed to reset password", cancellationToken);

    /// <inheritdoc />
    public Task<ApiResponse<object>> UpdateProfileAsync(
        Guid accountId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
        => PutAsync<object>(
            $"/api/identity/accounts/{accountId}/profile",
            new { request.FullName, request.PhoneNumber },
            "Failed to update profile", cancellationToken);

    #endregion

    #region System Profile Management

    /// <inheritdoc />
    public Task<ApiResponse<SystemProfileResponse>> GetSystemProfileAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
        => GetAsync<SystemProfileResponse>(
            $"/api/identity/accounts/{accountId}/system-profile",
            "Failed to retrieve system profile", cancellationToken);

    /// <inheritdoc />
    public Task<ApiResponse<object>> CreateSystemProfileAsync(
        Guid accountId,
        CreateSystemProfileWebRequest request,
        CancellationToken cancellationToken = default)
        => PostAsync<object>(
            $"/api/identity/accounts/{accountId}/system-profile", request,
            "Failed to create system profile", cancellationToken);

    /// <inheritdoc />
    public Task<ApiResponse<object>> UpdateSystemProfileAsync(
        Guid accountId,
        UpdateSystemProfileWebRequest request,
        CancellationToken cancellationToken = default)
        => PutAsync<object>(
            $"/api/identity/accounts/{accountId}/system-profile", request,
            "Failed to update system profile", cancellationToken);

    /// <inheritdoc />
    public Task<ApiResponse<object>> GrantSuperAdminAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
        => PostAsync<object>(
            $"/api/identity/accounts/{accountId}/system-profile/grant-super-admin",
            "Failed to grant super admin", cancellationToken);

    /// <inheritdoc />
    public Task<ApiResponse<object>> RevokeSuperAdminAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
        => PostAsync<object>(
            $"/api/identity/accounts/{accountId}/system-profile/revoke-super-admin",
            "Failed to revoke super admin", cancellationToken);

    #endregion

    #region Employee Profile Management

    /// <inheritdoc />
    public Task<ApiResponse<EmployeeProfileResponse>> GetEmployeeProfileAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
        => GetAsync<EmployeeProfileResponse>(
            $"/api/identity/accounts/{accountId}/employee-profile",
            "Failed to retrieve employee profile", cancellationToken);

    /// <inheritdoc />
    public Task<ApiResponse<object>> CreateEmployeeProfileAsync(
        Guid accountId,
        CreateEmployeeProfileWebRequest request,
        CancellationToken cancellationToken = default)
        => PostAsync<object>(
            $"/api/identity/accounts/{accountId}/employee-profile", request,
            "Failed to create employee profile", cancellationToken);

    /// <inheritdoc />
    public Task<ApiResponse<object>> UpdateEmployeeProfileAsync(
        Guid accountId,
        UpdateEmployeeProfileWebRequest request,
        CancellationToken cancellationToken = default)
        => PutAsync<object>(
            $"/api/identity/accounts/{accountId}/employee-profile", request,
            "Failed to update employee profile", cancellationToken);

    #endregion
}
