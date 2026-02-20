namespace HRM.Web.Services.Abstractions;

/// <summary>
/// Model for deserializing API error responses.
/// Supports both RFC 7807 Problem Details and DomainError format.
/// </summary>
internal sealed class ApiErrorResponse
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
    /// Get error code from either format.
    /// </summary>
    public string GetErrorCode() => Code ?? Type ?? "ApiError";

    /// <summary>
    /// Get error message from either format.
    /// </summary>
    public string GetErrorMessage() => Message ?? Detail ?? Title ?? "An error occurred";

    /// <summary>
    /// Get validation errors from either format.
    /// </summary>
    public Dictionary<string, string[]>? GetValidationErrors() => Details ?? Errors;
}
