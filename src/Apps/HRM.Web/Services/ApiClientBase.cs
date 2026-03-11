using System.Net.Http.Json;
using System.Text.Json;
using HRM.Web.Models;
using HRM.Web.Services.Abstractions;

namespace HRM.Web.Services;

/// <summary>
/// Base class for all module API clients.
/// Provides shared HTTP helpers (Get/Post/Put/Delete) and unified error handling.
/// </summary>
public abstract class ApiClientBase
{
    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected readonly HttpClient HttpClient;
    private readonly ILogger _logger;

    protected ApiClientBase(HttpClient httpClient, ILogger logger)
    {
        HttpClient = httpClient;
        _logger = logger;
    }

    protected async Task<ApiResponse<T>> GetAsync<T>(
        string url, string errorMessage, CancellationToken cancellationToken)
    {
        try
        {
            var response = await HttpClient.GetAsync(url, cancellationToken);
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

    protected async Task<ApiResponse<T>> PostAsync<T>(
        string url, object body, string errorMessage, CancellationToken cancellationToken)
    {
        try
        {
            var response = await HttpClient.PostAsJsonAsync(url, body, cancellationToken);
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

    /// <summary>POST with no request body (e.g. activate/suspend/unlock actions).</summary>
    protected async Task<ApiResponse<T>> PostAsync<T>(
        string url, string errorMessage, CancellationToken cancellationToken)
    {
        try
        {
            var response = await HttpClient.PostAsync(url, null, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                    return new ApiResponse<T> { IsSuccess = true };

                var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
                return new ApiResponse<T> { IsSuccess = true, Data = data };
            }
            return await HandleErrorResponseAsync<T>(response, errorMessage, cancellationToken);
        }
        catch (HttpRequestException ex) { return HandleNetworkError<T>(ex); }
        catch (Exception ex) { return HandleUnexpectedError<T>(ex); }
    }

    protected async Task<ApiResponse<T>> PutAsync<T>(
        string url, object body, string errorMessage, CancellationToken cancellationToken)
    {
        try
        {
            var response = await HttpClient.PutAsJsonAsync(url, body, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                    return new ApiResponse<T> { IsSuccess = true };

                var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
                return new ApiResponse<T> { IsSuccess = true, Data = data };
            }
            return await HandleErrorResponseAsync<T>(response, errorMessage, cancellationToken);
        }
        catch (HttpRequestException ex) { return HandleNetworkError<T>(ex); }
        catch (Exception ex) { return HandleUnexpectedError<T>(ex); }
    }

    protected async Task<ApiResponse<T>> DeleteAsync<T>(
        string url, string errorMessage, CancellationToken cancellationToken)
    {
        try
        {
            var response = await HttpClient.DeleteAsync(url, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                    return new ApiResponse<T> { IsSuccess = true };

                var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
                return new ApiResponse<T> { IsSuccess = true, Data = data };
            }
            return await HandleErrorResponseAsync<T>(response, errorMessage, cancellationToken);
        }
        catch (HttpRequestException ex) { return HandleNetworkError<T>(ex); }
        catch (Exception ex) { return HandleUnexpectedError<T>(ex); }
    }

    protected async Task<ApiResponse<T>> HandleErrorResponseAsync<T>(
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

    protected ApiResponse<T> HandleNetworkError<T>(HttpRequestException ex)
    {
        _logger.LogError(ex, "Network error while calling API");
        return new ApiResponse<T>
        {
            IsSuccess = false,
            ErrorCode = "NetworkError",
            ErrorMessage = "Failed to connect to API server. Please try again later."
        };
    }

    protected ApiResponse<T> HandleUnexpectedError<T>(Exception ex)
    {
        _logger.LogError(ex, "Unexpected error while calling API");
        return new ApiResponse<T>
        {
            IsSuccess = false,
            ErrorCode = "UnexpectedError",
            ErrorMessage = "An unexpected error occurred. Please contact support."
        };
    }
}
