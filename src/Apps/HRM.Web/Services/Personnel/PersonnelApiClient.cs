using System.Net.Http.Json;
using System.Text.Json;
using HRM.Web.Models;
using HRM.Web.Services.Abstractions;

namespace HRM.Web.Services.Personnel;

/// <summary>
/// HTTP client for Personnel module API endpoints.
/// Handles employee management operations.
/// </summary>
public sealed class PersonnelApiClient : IPersonnelApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PersonnelApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PersonnelApiClient(
        HttpClient httpClient,
        ILogger<PersonnelApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ApiResponse<PagedResult<EmployeeSummaryResponse>>> GetEmployeesAsync(
        string? searchTerm = null,
        string? status = null,
        Guid? companyId = null,
        Guid? departmentId = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = new List<string>
            {
                $"pageNumber={pageNumber}",
                $"pageSize={pageSize}"
            };

            if (!string.IsNullOrWhiteSpace(searchTerm))
                queryParams.Add($"searchTerm={Uri.EscapeDataString(searchTerm)}");

            if (!string.IsNullOrWhiteSpace(status))
                queryParams.Add($"status={Uri.EscapeDataString(status)}");

            if (companyId.HasValue)
                queryParams.Add($"companyId={companyId.Value}");

            if (departmentId.HasValue)
                queryParams.Add($"departmentId={departmentId.Value}");

            var url = $"/api/personnel/employees?{string.Join("&", queryParams)}";
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<PagedResult<EmployeeSummaryResponse>>(JsonOptions, cancellationToken);
                return new ApiResponse<PagedResult<EmployeeSummaryResponse>>
                {
                    IsSuccess = true,
                    Data = data ?? new PagedResult<EmployeeSummaryResponse>()
                };
            }

            return await HandleErrorResponseAsync<PagedResult<EmployeeSummaryResponse>>(response, "Failed to retrieve employees", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return HandleNetworkError<PagedResult<EmployeeSummaryResponse>>(ex);
        }
        catch (Exception ex)
        {
            return HandleUnexpectedError<PagedResult<EmployeeSummaryResponse>>(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ApiResponse<EmployeeSummaryResponse>> GetEmployeeByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/personnel/employees/{id}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<EmployeeSummaryResponse>(JsonOptions, cancellationToken);
                return new ApiResponse<EmployeeSummaryResponse>
                {
                    IsSuccess = true,
                    Data = data
                };
            }

            return await HandleErrorResponseAsync<EmployeeSummaryResponse>(response, "Failed to retrieve employee", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return HandleNetworkError<EmployeeSummaryResponse>(ex);
        }
        catch (Exception ex)
        {
            return HandleUnexpectedError<EmployeeSummaryResponse>(ex);
        }
    }

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
        _logger.LogError(ex, "Network error while calling Personnel API");
        return new ApiResponse<T>
        {
            IsSuccess = false,
            ErrorCode = "NetworkError",
            ErrorMessage = "Failed to connect to API server. Please try again later."
        };
    }

    private ApiResponse<T> HandleUnexpectedError<T>(Exception ex)
    {
        _logger.LogError(ex, "Unexpected error while calling Personnel API");
        return new ApiResponse<T>
        {
            IsSuccess = false,
            ErrorCode = "UnexpectedError",
            ErrorMessage = "An unexpected error occurred. Please contact support."
        };
    }

    #endregion
}
