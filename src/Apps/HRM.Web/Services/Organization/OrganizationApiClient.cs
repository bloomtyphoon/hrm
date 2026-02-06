using System.Net.Http.Json;
using System.Text.Json;
using HRM.Web.Models;
using HRM.Web.Services.Abstractions;

namespace HRM.Web.Services.Organization;

/// <summary>
/// HTTP client for Organization module API endpoints.
/// Handles company management operations.
/// </summary>
public sealed class OrganizationApiClient : IOrganizationApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OrganizationApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public OrganizationApiClient(
        HttpClient httpClient,
        ILogger<OrganizationApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<ApiResponse<CompanyResponse>> CreateCompanyAsync(
        CreateCompanyRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var apiRequest = new
            {
                request.Code,
                request.Name,
                request.TaxId
            };

            var response = await _httpClient.PostAsJsonAsync(
                "/api/organization/companies",
                apiRequest,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<CompanyResponse>(JsonOptions, cancellationToken);
                return new ApiResponse<CompanyResponse>
                {
                    IsSuccess = true,
                    Data = data
                };
            }

            return await HandleErrorResponseAsync<CompanyResponse>(response, "Failed to create company", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return HandleNetworkError<CompanyResponse>(ex);
        }
        catch (Exception ex)
        {
            return HandleUnexpectedError<CompanyResponse>(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ApiResponse<IReadOnlyList<CompanyResponse>>> GetCompaniesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/organization/companies", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<List<CompanyResponse>>(JsonOptions, cancellationToken);
                return new ApiResponse<IReadOnlyList<CompanyResponse>>
                {
                    IsSuccess = true,
                    Data = data ?? []
                };
            }

            return await HandleErrorResponseAsync<IReadOnlyList<CompanyResponse>>(response, "Failed to retrieve companies", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return HandleNetworkError<IReadOnlyList<CompanyResponse>>(ex);
        }
        catch (Exception ex)
        {
            return HandleUnexpectedError<IReadOnlyList<CompanyResponse>>(ex);
        }
    }

    /// <inheritdoc />
    public async Task<ApiResponse<CompanyResponse>> GetCompanyByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/organization/companies/{id}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<CompanyResponse>(JsonOptions, cancellationToken);
                return new ApiResponse<CompanyResponse>
                {
                    IsSuccess = true,
                    Data = data
                };
            }

            return await HandleErrorResponseAsync<CompanyResponse>(response, "Failed to retrieve company", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            return HandleNetworkError<CompanyResponse>(ex);
        }
        catch (Exception ex)
        {
            return HandleUnexpectedError<CompanyResponse>(ex);
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
        _logger.LogError(ex, "Network error while calling Organization API");
        return new ApiResponse<T>
        {
            IsSuccess = false,
            ErrorCode = "NetworkError",
            ErrorMessage = "Failed to connect to API server. Please try again later."
        };
    }

    private ApiResponse<T> HandleUnexpectedError<T>(Exception ex)
    {
        _logger.LogError(ex, "Unexpected error while calling Organization API");
        return new ApiResponse<T>
        {
            IsSuccess = false,
            ErrorCode = "UnexpectedError",
            ErrorMessage = "An unexpected error occurred. Please contact support."
        };
    }

    #endregion
}
