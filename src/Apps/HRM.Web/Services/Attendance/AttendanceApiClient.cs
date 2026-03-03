using System.Net.Http.Json;
using System.Text.Json;
using HRM.Web.Models;
using HRM.Web.Services.Abstractions;

namespace HRM.Web.Services.Attendance;

public sealed class AttendanceApiClient : IAttendanceApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AttendanceApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AttendanceApiClient(
        HttpClient httpClient,
        ILogger<AttendanceApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ApiResponse<Guid>> CheckInAsync(
        DateTime? checkInTimeUtc = null,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<Guid>("/api/attendance/check-in",
            new { CheckInTimeUtc = checkInTimeUtc, Notes = notes },
            "Failed to check in", cancellationToken);
    }

    public async Task<ApiResponse<Guid>> CheckOutAsync(
        DateTime? checkOutTimeUtc = null,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<Guid>("/api/attendance/check-out",
            new { CheckOutTimeUtc = checkOutTimeUtc, Notes = notes },
            "Failed to check out", cancellationToken);
    }

    public async Task<ApiResponse<PagedResult<AttendanceSummaryResponse>>> GetMyAttendanceAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>
        {
            $"pageNumber={pageNumber}",
            $"pageSize={pageSize}"
        };

        if (fromDate.HasValue)
            queryParams.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
        if (toDate.HasValue)
            queryParams.Add($"toDate={toDate.Value:yyyy-MM-dd}");

        var url = $"/api/attendance/me?{string.Join("&", queryParams)}";
        return await GetAsync<PagedResult<AttendanceSummaryResponse>>(url, "Failed to retrieve attendance records", cancellationToken);
    }

    public async Task<ApiResponse<PagedResult<AttendanceSummaryResponse>>> GetEmployeeAttendanceAsync(
        Guid employeeId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>
        {
            $"pageNumber={pageNumber}",
            $"pageSize={pageSize}"
        };

        if (fromDate.HasValue)
            queryParams.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
        if (toDate.HasValue)
            queryParams.Add($"toDate={toDate.Value:yyyy-MM-dd}");

        var url = $"/api/attendance/employees/{employeeId}?{string.Join("&", queryParams)}";
        return await GetAsync<PagedResult<AttendanceSummaryResponse>>(url, "Failed to retrieve employee attendance", cancellationToken);
    }

    public async Task<ApiResponse<AttendanceDetailResponse>> GetAttendanceByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await GetAsync<AttendanceDetailResponse>($"/api/attendance/records/{id}", "Failed to retrieve attendance record", cancellationToken);
    }

    public async Task<ApiResponse<Guid>> RecordManualAttendanceAsync(
        Guid employeeId,
        DateOnly date,
        DateTime checkInTimeUtc,
        DateTime checkOutTimeUtc,
        Guid? companyId = null,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<Guid>("/api/attendance/records",
            new
            {
                EmployeeId = employeeId,
                Date = date,
                CheckInTimeUtc = checkInTimeUtc,
                CheckOutTimeUtc = checkOutTimeUtc,
                CompanyId = companyId,
                Notes = notes
            },
            "Failed to record manual attendance", cancellationToken);
    }

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
        _logger.LogError(ex, "Network error while calling Attendance API");
        return new ApiResponse<T>
        {
            IsSuccess = false,
            ErrorCode = "NetworkError",
            ErrorMessage = "Failed to connect to API server. Please try again later."
        };
    }

    private ApiResponse<T> HandleUnexpectedError<T>(Exception ex)
    {
        _logger.LogError(ex, "Unexpected error while calling Attendance API");
        return new ApiResponse<T>
        {
            IsSuccess = false,
            ErrorCode = "UnexpectedError",
            ErrorMessage = "An unexpected error occurred. Please contact support."
        };
    }

    #endregion
}
