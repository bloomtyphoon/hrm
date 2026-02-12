namespace HRM.Modules.Identity.Api.Contracts;

/// <summary>
/// Request DTO for updating account profile.
/// </summary>
public sealed record UpdateProfileRequest(
    string FullName,
    string? PhoneNumber
);
