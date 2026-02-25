using System.Net.Http.Json;
using System.Text.Json;
using HRM.Web.Models;
using HRM.Web.Services.Abstractions;

namespace HRM.Web.Services.Organization;

/// <summary>
/// HTTP client for Organization module API endpoints.
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

    #region Companies

    public async Task<ApiResponse<CompanyResponse>> CreateCompanyAsync(
        CreateCompanyRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var apiRequest = new { request.Code, request.Name, request.TaxId };
            var response = await _httpClient.PostAsJsonAsync(
                "/api/organization/companies", apiRequest, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<CompanyResponse>(JsonOptions, cancellationToken);
                return new ApiResponse<CompanyResponse> { IsSuccess = true, Data = data };
            }

            return await HandleErrorResponseAsync<CompanyResponse>(response, "Failed to create company", cancellationToken);
        }
        catch (HttpRequestException ex) { return HandleNetworkError<CompanyResponse>(ex); }
        catch (Exception ex) { return HandleUnexpectedError<CompanyResponse>(ex); }
    }

    public async Task<ApiResponse<IReadOnlyList<CompanyResponse>>> GetCompaniesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/organization/companies", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<List<CompanyResponse>>(JsonOptions, cancellationToken);
                return new ApiResponse<IReadOnlyList<CompanyResponse>> { IsSuccess = true, Data = data ?? [] };
            }

            return await HandleErrorResponseAsync<IReadOnlyList<CompanyResponse>>(response, "Failed to retrieve companies", cancellationToken);
        }
        catch (HttpRequestException ex) { return HandleNetworkError<IReadOnlyList<CompanyResponse>>(ex); }
        catch (Exception ex) { return HandleUnexpectedError<IReadOnlyList<CompanyResponse>>(ex); }
    }

    public async Task<ApiResponse<CompanyResponse>> GetCompanyByIdAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return await GetAsync<CompanyResponse>($"/api/organization/companies/{id}", "Failed to retrieve company", cancellationToken);
    }

    public async Task<ApiResponse<CompanyResponse>> UpdateCompanyAsync(
        Guid id, string name, string? taxId, CancellationToken cancellationToken = default)
    {
        return await PutAsync<CompanyResponse>($"/api/organization/companies/{id}",
            new { Name = name, TaxId = taxId }, "Failed to update company", cancellationToken);
    }

    public async Task<ApiResponse<CompanyResponse>> ActivateCompanyAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return await PutAsync<CompanyResponse>($"/api/organization/companies/{id}/activate",
            new { }, "Failed to activate company", cancellationToken);
    }

    public async Task<ApiResponse<CompanyResponse>> DeactivateCompanyAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return await PutAsync<CompanyResponse>($"/api/organization/companies/{id}/deactivate",
            new { }, "Failed to deactivate company", cancellationToken);
    }

    #endregion

    #region Departments

    public async Task<ApiResponse<IReadOnlyList<DepartmentResponse>>> GetDepartmentsByCompanyAsync(
        Guid companyId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"/api/organization/departments?companyId={companyId}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<List<DepartmentResponse>>(JsonOptions, cancellationToken);
                return new ApiResponse<IReadOnlyList<DepartmentResponse>> { IsSuccess = true, Data = data ?? [] };
            }

            return await HandleErrorResponseAsync<IReadOnlyList<DepartmentResponse>>(response, "Failed to retrieve departments", cancellationToken);
        }
        catch (HttpRequestException ex) { return HandleNetworkError<IReadOnlyList<DepartmentResponse>>(ex); }
        catch (Exception ex) { return HandleUnexpectedError<IReadOnlyList<DepartmentResponse>>(ex); }
    }

    public async Task<ApiResponse<DepartmentResponse>> GetDepartmentByIdAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return await GetAsync<DepartmentResponse>($"/api/organization/departments/{id}", "Failed to retrieve department", cancellationToken);
    }

    public async Task<ApiResponse<DepartmentResponse>> CreateDepartmentAsync(
        Guid companyId, string code, string name, Guid? parentDepartmentId,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<DepartmentResponse>("/api/organization/departments",
            new { CompanyId = companyId, Code = code, Name = name, ParentDepartmentId = parentDepartmentId },
            "Failed to create department", cancellationToken);
    }

    public async Task<ApiResponse<DepartmentResponse>> UpdateDepartmentAsync(
        Guid id, string name, CancellationToken cancellationToken = default)
    {
        return await PutAsync<DepartmentResponse>($"/api/organization/departments/{id}",
            new { Name = name }, "Failed to update department", cancellationToken);
    }

    public async Task<ApiResponse<DepartmentResponse>> ActivateDepartmentAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return await PutAsync<DepartmentResponse>($"/api/organization/departments/{id}/activate",
            new { }, "Failed to activate department", cancellationToken);
    }

    public async Task<ApiResponse<DepartmentResponse>> DeactivateDepartmentAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return await PutAsync<DepartmentResponse>($"/api/organization/departments/{id}/deactivate",
            new { }, "Failed to deactivate department", cancellationToken);
    }

    #endregion

    #region Positions

    public async Task<ApiResponse<IReadOnlyList<PositionResponse>>> GetPositionsByCompanyAsync(
        Guid companyId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"/api/organization/positions?companyId={companyId}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<List<PositionResponse>>(JsonOptions, cancellationToken);
                return new ApiResponse<IReadOnlyList<PositionResponse>> { IsSuccess = true, Data = data ?? [] };
            }

            return await HandleErrorResponseAsync<IReadOnlyList<PositionResponse>>(response, "Failed to retrieve positions", cancellationToken);
        }
        catch (HttpRequestException ex) { return HandleNetworkError<IReadOnlyList<PositionResponse>>(ex); }
        catch (Exception ex) { return HandleUnexpectedError<IReadOnlyList<PositionResponse>>(ex); }
    }

    public async Task<ApiResponse<PositionResponse>> GetPositionByIdAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return await GetAsync<PositionResponse>($"/api/organization/positions/{id}", "Failed to retrieve position", cancellationToken);
    }

    public async Task<ApiResponse<PositionResponse>> CreatePositionAsync(
        Guid companyId, string code, string title, int positionLevel, bool isManagement,
        Guid? departmentId, string? description, int? maxHeadcount,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<PositionResponse>("/api/organization/positions",
            new
            {
                CompanyId = companyId, Code = code, Title = title, PositionLevel = positionLevel,
                IsManagement = isManagement, DepartmentId = departmentId,
                Description = description, MaxHeadcount = maxHeadcount
            },
            "Failed to create position", cancellationToken);
    }

    public async Task<ApiResponse<PositionResponse>> UpdatePositionAsync(
        Guid id, string title, int positionLevel, bool isManagement,
        string? description, int? maxHeadcount,
        CancellationToken cancellationToken = default)
    {
        return await PutAsync<PositionResponse>($"/api/organization/positions/{id}",
            new
            {
                Title = title, PositionLevel = positionLevel, IsManagement = isManagement,
                Description = description, MaxHeadcount = maxHeadcount
            },
            "Failed to update position", cancellationToken);
    }

    public async Task<ApiResponse<PositionResponse>> ActivatePositionAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return await PutAsync<PositionResponse>($"/api/organization/positions/{id}/activate",
            new { }, "Failed to activate position", cancellationToken);
    }

    public async Task<ApiResponse<PositionResponse>> DeactivatePositionAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return await PutAsync<PositionResponse>($"/api/organization/positions/{id}/deactivate",
            new { }, "Failed to deactivate position", cancellationToken);
    }

    public async Task<ApiResponse<PositionResponse>> ClosePositionAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return await PutAsync<PositionResponse>($"/api/organization/positions/{id}/close",
            new { }, "Failed to close position", cancellationToken);
    }

    #endregion

    #region Private Helpers

    private async Task<ApiResponse<T>> GetAsync<T>(string url, string errorMessage, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
                return new ApiResponse<T> { IsSuccess = true, Data = data };
            }
            return await HandleErrorResponseAsync<T>(response, errorMessage, cancellationToken);
        }
        catch (HttpRequestException ex) { return HandleNetworkError<T>(ex); }
        catch (Exception ex) { return HandleUnexpectedError<T>(ex); }
    }

    private async Task<ApiResponse<T>> PostAsync<T>(string url, object body, string errorMessage, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(url, body, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
                return new ApiResponse<T> { IsSuccess = true, Data = data };
            }
            return await HandleErrorResponseAsync<T>(response, errorMessage, cancellationToken);
        }
        catch (HttpRequestException ex) { return HandleNetworkError<T>(ex); }
        catch (Exception ex) { return HandleUnexpectedError<T>(ex); }
    }

    private async Task<ApiResponse<T>> PutAsync<T>(string url, object body, string errorMessage, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync(url, body, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
                return new ApiResponse<T> { IsSuccess = true, Data = data };
            }
            return await HandleErrorResponseAsync<T>(response, errorMessage, cancellationToken);
        }
        catch (HttpRequestException ex) { return HandleNetworkError<T>(ex); }
        catch (Exception ex) { return HandleUnexpectedError<T>(ex); }
    }

    private async Task<ApiResponse<T>> HandleErrorResponseAsync<T>(
        HttpResponseMessage response, string defaultMessage, CancellationToken cancellationToken)
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
