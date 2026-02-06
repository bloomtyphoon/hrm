using HRM.Web.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace HRM.Web.Services;

/// <summary>
/// HTTP client for calling HRM.Api endpoints
/// Handles serialization, error handling, and response mapping
/// </summary>
public interface IApiClient
{
    Task<ApiResponse<AccountResponse>> RegisterAccountAsync(
        RegisterAccountRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<object>> LogoutAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PagedResult<AccountSummary>>> GetAccountsAsync(
        string? searchTerm = null,
        string? status = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    // Company operations
    Task<ApiResponse<CompanyResponse>> CreateCompanyAsync(
        CreateCompanyRequest request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<IReadOnlyList<CompanyResponse>>> GetCompaniesAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResponse<CompanyResponse>> GetCompanyByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<AccountResponse>> ActivateAccountAsync(
        Guid accountId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<List<SessionInfo>>> GetActiveSessionsAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResponse<object>> RevokeSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<RevokeAllSessionsResult>> RevokeAllSessionsExceptCurrentAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResponse<LoginResponse>> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);
}

public sealed class ApiClient : IApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ApiClient> _logger;

    public ApiClient(
        IHttpClientFactory httpClientFactory,
        ILogger<ApiClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Register a new operator via HRM.Api
    /// </summary>
    public async Task<ApiResponse<AccountResponse>> RegisterAccountAsync(
        RegisterAccountRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("HRM.Api");

            // Map to API contract (remove ConfirmPassword, include FullName and PhoneNumber)
            var apiRequest = new
            {
                request.Username,
                request.Email,
                request.Password,
                request.FullName,
                request.PhoneNumber
            };

            var response = await httpClient.PostAsJsonAsync(
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

            // Handle error responses (400, 500, etc.)
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);

            // Try to parse ProblemDetails (RFC 7807)
            try
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var apiError = JsonSerializer.Deserialize<ApiErrorResponse>(errorContent, jsonOptions);
                return new ApiResponse<AccountResponse>
                {
                    IsSuccess = false,
                    ErrorCode = apiError?.GetErrorCode() ?? "ApiError",
                    ErrorMessage = apiError?.GetErrorMessage() ?? "An error occurred while processing your request",
                    ValidationErrors = apiError?.GetValidationErrors()
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize ProblemDetails. Raw response: {ErrorContent}", errorContent);

                // Fallback if not ProblemDetails format
                return new ApiResponse<AccountResponse>
                {
                    IsSuccess = false,
                    ErrorCode = "ApiError",
                    ErrorMessage = $"Server returned {(int)response.StatusCode}: {errorContent}"
                };
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while calling HRM.Api");
            return new ApiResponse<AccountResponse>
            {
                IsSuccess = false,
                ErrorCode = "NetworkError",
                ErrorMessage = "Failed to connect to API server. Please try again later."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while calling HRM.Api");
            return new ApiResponse<AccountResponse>
            {
                IsSuccess = false,
                ErrorCode = "UnexpectedError",
                ErrorMessage = "An unexpected error occurred. Please contact support."
            };
        }
    }

    /// <summary>
    /// Login operator via HRM.Api
    /// </summary>
    public async Task<ApiResponse<LoginResponse>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("HRM.Api");

            var apiRequest = new
            {
                request.UsernameOrEmail,
                request.Password,
                request.RememberMe
            };

            var response = await httpClient.PostAsJsonAsync(
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

            // Handle error responses
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);

            try
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var apiError = JsonSerializer.Deserialize<ApiErrorResponse>(errorContent, jsonOptions);
                return new ApiResponse<LoginResponse>
                {
                    IsSuccess = false,
                    ErrorCode = apiError?.GetErrorCode() ?? "LoginError",
                    ErrorMessage = apiError?.GetErrorMessage() ?? "Invalid username or password",
                    ValidationErrors = apiError?.GetValidationErrors()
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize ProblemDetails. Raw response: {ErrorContent}", errorContent);

                return new ApiResponse<LoginResponse>
                {
                    IsSuccess = false,
                    ErrorCode = "LoginError",
                    ErrorMessage = $"Server returned {(int)response.StatusCode}: {errorContent}"
                };
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while calling HRM.Api");
            return new ApiResponse<LoginResponse>
            {
                IsSuccess = false,
                ErrorCode = "NetworkError",
                ErrorMessage = "Failed to connect to API server. Please try again later."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while calling HRM.Api");
            return new ApiResponse<LoginResponse>
            {
                IsSuccess = false,
                ErrorCode = "UnexpectedError",
                ErrorMessage = "An unexpected error occurred. Please contact support."
            };
        }
    }

    /// <summary>
    /// Logout current operator via HRM.Api
    /// </summary>
    public async Task<ApiResponse<object>> LogoutAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("HRM.Api");

            var response = await httpClient.PostAsync(
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

            // Handle error responses
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);

            try
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var apiError = JsonSerializer.Deserialize<ApiErrorResponse>(errorContent, jsonOptions);
                return new ApiResponse<object>
                {
                    IsSuccess = false,
                    ErrorCode = apiError?.GetErrorCode() ?? "LogoutError",
                    ErrorMessage = apiError?.GetErrorMessage() ?? "Failed to logout",
                    ValidationErrors = apiError?.GetValidationErrors()
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize ProblemDetails. Raw response: {ErrorContent}", errorContent);

                return new ApiResponse<object>
                {
                    IsSuccess = false,
                    ErrorCode = "LogoutError",
                    ErrorMessage = $"Server returned {(int)response.StatusCode}: {errorContent}"
                };
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while calling HRM.Api");
            return new ApiResponse<object>
            {
                IsSuccess = false,
                ErrorCode = "NetworkError",
                ErrorMessage = "Failed to connect to API server. Please try again later."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while calling HRM.Api");
            return new ApiResponse<object>
            {
                IsSuccess = false,
                ErrorCode = "UnexpectedError",
                ErrorMessage = "An unexpected error occurred. Please contact support."
            };
        }
    }

    /// <summary>
    /// Get paginated list of operators via HRM.Api
    /// </summary>
    public async Task<ApiResponse<PagedResult<AccountSummary>>> GetAccountsAsync(
        string? searchTerm = null,
        string? status = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("HRM.Api");

            // Build query string
            var queryParams = new List<string>();
            if (!string.IsNullOrWhiteSpace(searchTerm))
                queryParams.Add($"searchTerm={Uri.EscapeDataString(searchTerm)}");
            if (!string.IsNullOrWhiteSpace(status))
                queryParams.Add($"status={Uri.EscapeDataString(status)}");
            queryParams.Add($"pageNumber={pageNumber}");
            queryParams.Add($"pageSize={pageSize}");

            var queryString = string.Join("&", queryParams);
            var url = $"/api/identity/accounts?{queryString}";

            var response = await httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var data = await response.Content.ReadFromJsonAsync<PagedResult<AccountSummary>>(jsonOptions, cancellationToken);
                return new ApiResponse<PagedResult<AccountSummary>>
                {
                    IsSuccess = true,
                    Data = data ?? new PagedResult<AccountSummary>()
                };
            }

            // Handle error responses
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);

            try
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var apiError = JsonSerializer.Deserialize<ApiErrorResponse>(errorContent, jsonOptions);
                return new ApiResponse<PagedResult<AccountSummary>>
                {
                    IsSuccess = false,
                    ErrorCode = apiError?.GetErrorCode() ?? "ApiError",
                    ErrorMessage = apiError?.GetErrorMessage() ?? "Failed to retrieve accounts",
                    ValidationErrors = apiError?.GetValidationErrors()
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize ProblemDetails. Raw response: {ErrorContent}", errorContent);

                return new ApiResponse<PagedResult<AccountSummary>>
                {
                    IsSuccess = false,
                    ErrorCode = "ApiError",
                    ErrorMessage = $"Server returned {(int)response.StatusCode}: {errorContent}"
                };
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while calling HRM.Api");
            return new ApiResponse<PagedResult<AccountSummary>>
            {
                IsSuccess = false,
                ErrorCode = "NetworkError",
                ErrorMessage = "Failed to connect to API server. Please try again later."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while calling HRM.Api");
            return new ApiResponse<PagedResult<AccountSummary>>
            {
                IsSuccess = false,
                ErrorCode = "UnexpectedError",
                ErrorMessage = "An unexpected error occurred. Please contact support."
            };
        }
    }

    /// <summary>
    /// Activate a pending account via HRM.Api
    /// </summary>
    public async Task<ApiResponse<AccountResponse>> ActivateAccountAsync(
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("HRM.Api");

            var response = await httpClient.PostAsync(
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

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);

            try
            {
                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var apiError = JsonSerializer.Deserialize<ApiErrorResponse>(errorContent, jsonOptions);
                return new ApiResponse<AccountResponse>
                {
                    IsSuccess = false,
                    ErrorCode = apiError?.GetErrorCode() ?? "ApiError",
                    ErrorMessage = apiError?.GetErrorMessage() ?? "Failed to activate account",
                    ValidationErrors = apiError?.GetValidationErrors()
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize error response: {ErrorContent}", errorContent);
                return new ApiResponse<AccountResponse>
                {
                    IsSuccess = false,
                    ErrorCode = "ApiError",
                    ErrorMessage = $"Server returned {(int)response.StatusCode}: {errorContent}"
                };
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while activating account");
            return new ApiResponse<AccountResponse>
            {
                IsSuccess = false,
                ErrorCode = "NetworkError",
                ErrorMessage = "Failed to connect to API server. Please try again later."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while activating account");
            return new ApiResponse<AccountResponse>
            {
                IsSuccess = false,
                ErrorCode = "UnexpectedError",
                ErrorMessage = "An unexpected error occurred. Please contact support."
            };
        }
    }

    /// <summary>
    /// Get active sessions for current user via HRM.Api
    /// </summary>
    public async Task<ApiResponse<List<SessionInfo>>> GetActiveSessionsAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("HRM.Api");

            var response = await httpClient.GetAsync(
                "/api/identity/auth/sessions",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var data = await response.Content.ReadFromJsonAsync<List<SessionInfo>>(jsonOptions, cancellationToken);
                return new ApiResponse<List<SessionInfo>>
                {
                    IsSuccess = true,
                    Data = data ?? new List<SessionInfo>()
                };
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);

            try
            {
                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var apiError = JsonSerializer.Deserialize<ApiErrorResponse>(errorContent, jsonOptions);
                return new ApiResponse<List<SessionInfo>>
                {
                    IsSuccess = false,
                    ErrorCode = apiError?.GetErrorCode() ?? "ApiError",
                    ErrorMessage = apiError?.GetErrorMessage() ?? "Failed to retrieve sessions",
                    ValidationErrors = apiError?.GetValidationErrors()
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize error response: {ErrorContent}", errorContent);
                return new ApiResponse<List<SessionInfo>>
                {
                    IsSuccess = false,
                    ErrorCode = "ApiError",
                    ErrorMessage = $"Server returned {(int)response.StatusCode}: {errorContent}"
                };
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while getting sessions");
            return new ApiResponse<List<SessionInfo>>
            {
                IsSuccess = false,
                ErrorCode = "NetworkError",
                ErrorMessage = "Failed to connect to API server. Please try again later."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while getting sessions");
            return new ApiResponse<List<SessionInfo>>
            {
                IsSuccess = false,
                ErrorCode = "UnexpectedError",
                ErrorMessage = "An unexpected error occurred. Please contact support."
            };
        }
    }

    /// <summary>
    /// Revoke a specific session via HRM.Api
    /// </summary>
    public async Task<ApiResponse<object>> RevokeSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("HRM.Api");

            var response = await httpClient.DeleteAsync(
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

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);

            try
            {
                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var apiError = JsonSerializer.Deserialize<ApiErrorResponse>(errorContent, jsonOptions);
                return new ApiResponse<object>
                {
                    IsSuccess = false,
                    ErrorCode = apiError?.GetErrorCode() ?? "ApiError",
                    ErrorMessage = apiError?.GetErrorMessage() ?? "Failed to revoke session",
                    ValidationErrors = apiError?.GetValidationErrors()
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize error response: {ErrorContent}", errorContent);
                return new ApiResponse<object>
                {
                    IsSuccess = false,
                    ErrorCode = "ApiError",
                    ErrorMessage = $"Server returned {(int)response.StatusCode}: {errorContent}"
                };
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while revoking session");
            return new ApiResponse<object>
            {
                IsSuccess = false,
                ErrorCode = "NetworkError",
                ErrorMessage = "Failed to connect to API server. Please try again later."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while revoking session");
            return new ApiResponse<object>
            {
                IsSuccess = false,
                ErrorCode = "UnexpectedError",
                ErrorMessage = "An unexpected error occurred. Please contact support."
            };
        }
    }

    /// <summary>
    /// Create a new company via HRM.Api
    /// </summary>
    public async Task<ApiResponse<CompanyResponse>> CreateCompanyAsync(
        CreateCompanyRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("HRM.Api");

            var apiRequest = new
            {
                request.Code,
                request.Name,
                request.TaxId
            };

            var response = await httpClient.PostAsJsonAsync(
                "/api/organization/companies",
                apiRequest,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var data = await response.Content.ReadFromJsonAsync<CompanyResponse>(jsonOptions, cancellationToken);
                return new ApiResponse<CompanyResponse>
                {
                    IsSuccess = true,
                    Data = data
                };
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);

            try
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var apiError = JsonSerializer.Deserialize<ApiErrorResponse>(errorContent, jsonOptions);
                return new ApiResponse<CompanyResponse>
                {
                    IsSuccess = false,
                    ErrorCode = apiError?.GetErrorCode() ?? "ApiError",
                    ErrorMessage = apiError?.GetErrorMessage() ?? "Failed to create company",
                    ValidationErrors = apiError?.GetValidationErrors()
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize ProblemDetails. Raw response: {ErrorContent}", errorContent);
                return new ApiResponse<CompanyResponse>
                {
                    IsSuccess = false,
                    ErrorCode = "ApiError",
                    ErrorMessage = $"Server returned {(int)response.StatusCode}: {errorContent}"
                };
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while calling HRM.Api");
            return new ApiResponse<CompanyResponse>
            {
                IsSuccess = false,
                ErrorCode = "NetworkError",
                ErrorMessage = "Failed to connect to API server. Please try again later."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while calling HRM.Api");
            return new ApiResponse<CompanyResponse>
            {
                IsSuccess = false,
                ErrorCode = "UnexpectedError",
                ErrorMessage = "An unexpected error occurred. Please contact support."
            };
        }
    }

    /// <summary>
    /// Get all companies via HRM.Api
    /// </summary>
    public async Task<ApiResponse<IReadOnlyList<CompanyResponse>>> GetCompaniesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("HRM.Api");

            var response = await httpClient.GetAsync("/api/organization/companies", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var data = await response.Content.ReadFromJsonAsync<List<CompanyResponse>>(jsonOptions, cancellationToken);
                return new ApiResponse<IReadOnlyList<CompanyResponse>>
                {
                    IsSuccess = true,
                    Data = data ?? []
                };
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);

            try
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var apiError = JsonSerializer.Deserialize<ApiErrorResponse>(errorContent, jsonOptions);
                return new ApiResponse<IReadOnlyList<CompanyResponse>>
                {
                    IsSuccess = false,
                    ErrorCode = apiError?.GetErrorCode() ?? "ApiError",
                    ErrorMessage = apiError?.GetErrorMessage() ?? "Failed to retrieve companies",
                    ValidationErrors = apiError?.GetValidationErrors()
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize ProblemDetails. Raw response: {ErrorContent}", errorContent);
                return new ApiResponse<IReadOnlyList<CompanyResponse>>
                {
                    IsSuccess = false,
                    ErrorCode = "ApiError",
                    ErrorMessage = $"Server returned {(int)response.StatusCode}: {errorContent}"
                };
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while calling HRM.Api");
            return new ApiResponse<IReadOnlyList<CompanyResponse>>
            {
                IsSuccess = false,
                ErrorCode = "NetworkError",
                ErrorMessage = "Failed to connect to API server. Please try again later."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while calling HRM.Api");
            return new ApiResponse<IReadOnlyList<CompanyResponse>>
            {
                IsSuccess = false,
                ErrorCode = "UnexpectedError",
                ErrorMessage = "An unexpected error occurred. Please contact support."
            };
        }
    }

    /// <summary>
    /// Get company by ID via HRM.Api
    /// </summary>
    public async Task<ApiResponse<CompanyResponse>> GetCompanyByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("HRM.Api");

            var response = await httpClient.GetAsync($"/api/organization/companies/{id}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var data = await response.Content.ReadFromJsonAsync<CompanyResponse>(jsonOptions, cancellationToken);
                return new ApiResponse<CompanyResponse>
                {
                    IsSuccess = true,
                    Data = data
                };
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);

            try
            {
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var apiError = JsonSerializer.Deserialize<ApiErrorResponse>(errorContent, jsonOptions);
                return new ApiResponse<CompanyResponse>
                {
                    IsSuccess = false,
                    ErrorCode = apiError?.GetErrorCode() ?? "ApiError",
                    ErrorMessage = apiError?.GetErrorMessage() ?? "Failed to retrieve company",
                    ValidationErrors = apiError?.GetValidationErrors()
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize ProblemDetails. Raw response: {ErrorContent}", errorContent);
                return new ApiResponse<CompanyResponse>
                {
                    IsSuccess = false,
                    ErrorCode = "ApiError",
                    ErrorMessage = $"Server returned {(int)response.StatusCode}: {errorContent}"
                };
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while calling HRM.Api");
            return new ApiResponse<CompanyResponse>
            {
                IsSuccess = false,
                ErrorCode = "NetworkError",
                ErrorMessage = "Failed to connect to API server. Please try again later."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while calling HRM.Api");
            return new ApiResponse<CompanyResponse>
            {
                IsSuccess = false,
                ErrorCode = "UnexpectedError",
                ErrorMessage = "An unexpected error occurred. Please contact support."
            };
        }
    }

    /// <summary>
    /// Revoke all sessions except current via HRM.Api
    /// </summary>
    public async Task<ApiResponse<RevokeAllSessionsResult>> RevokeAllSessionsExceptCurrentAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("HRM.Api");

            var response = await httpClient.PostAsync(
                "/api/identity/auth/sessions/revoke-all-except-current",
                null,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var data = await response.Content.ReadFromJsonAsync<RevokeAllSessionsResult>(jsonOptions, cancellationToken);
                return new ApiResponse<RevokeAllSessionsResult>
                {
                    IsSuccess = true,
                    Data = data
                };
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);

            try
            {
                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var apiError = JsonSerializer.Deserialize<ApiErrorResponse>(errorContent, jsonOptions);
                return new ApiResponse<RevokeAllSessionsResult>
                {
                    IsSuccess = false,
                    ErrorCode = apiError?.GetErrorCode() ?? "ApiError",
                    ErrorMessage = apiError?.GetErrorMessage() ?? "Failed to revoke sessions",
                    ValidationErrors = apiError?.GetValidationErrors()
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize error response: {ErrorContent}", errorContent);
                return new ApiResponse<RevokeAllSessionsResult>
                {
                    IsSuccess = false,
                    ErrorCode = "ApiError",
                    ErrorMessage = $"Server returned {(int)response.StatusCode}: {errorContent}"
                };
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error while revoking all sessions");
            return new ApiResponse<RevokeAllSessionsResult>
            {
                IsSuccess = false,
                ErrorCode = "NetworkError",
                ErrorMessage = "Failed to connect to API server. Please try again later."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while revoking all sessions");
            return new ApiResponse<RevokeAllSessionsResult>
            {
                IsSuccess = false,
                ErrorCode = "UnexpectedError",
                ErrorMessage = "An unexpected error occurred. Please contact support."
            };
        }
    }

    /// <summary>
    /// Refresh access token using a valid refresh token via HRM.Api.
    /// Uses a dedicated HttpClient without AuthTokenHandler to avoid recursion.
    /// </summary>
    public async Task<ApiResponse<LoginResponse>> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Use the named HttpClient (which goes through AuthTokenHandler),
            // but the refresh endpoint doesn't require a valid access token — it validates the refresh token.
            var httpClient = _httpClientFactory.CreateClient("HRM.Api");

            var payload = new { refreshToken };

            var response = await httpClient.PostAsJsonAsync(
                "/api/identity/auth/refresh",
                payload,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var data = await response.Content.ReadFromJsonAsync<LoginResponse>(jsonOptions, cancellationToken);
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

    /// <summary>
    /// Model for deserializing API error responses
    /// Supports both RFC 7807 Problem Details and DomainError format
    /// </summary>
    private sealed class ApiErrorResponse
    {
        // RFC 7807 Problem Details format
        public string? Type { get; set; }
        public string? Title { get; set; }
        public int Status { get; set; }
        public string? Detail { get; set; }
        public Dictionary<string, string[]>? Errors { get; set; }

        // DomainError format (from HRM.Api)
        public string? Code { get; set; }
        public string? Message { get; set; }
        public Dictionary<string, string[]>? Details { get; set; }

        /// <summary>
        /// Get error code from either format
        /// </summary>
        public string GetErrorCode() => Code ?? Type ?? "ApiError";

        /// <summary>
        /// Get error message from either format
        /// </summary>
        public string GetErrorMessage() => Message ?? Detail ?? Title ?? "An error occurred";

        /// <summary>
        /// Get validation errors from either format
        /// </summary>
        public Dictionary<string, string[]>? GetValidationErrors() => Details ?? Errors;
    }
}
