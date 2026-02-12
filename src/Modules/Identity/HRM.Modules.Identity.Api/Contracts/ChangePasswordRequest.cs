namespace HRM.Modules.Identity.Api.Contracts;

/// <summary>
/// Request DTO for changing account password.
/// </summary>
public sealed record ChangePasswordRequest(
    string? CurrentPassword,
    string NewPassword,
    bool IsAdminReset = false
);
