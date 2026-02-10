namespace HRM.Modules.Identity.Api.Contracts;

/// <summary>
/// Request DTO for updating a system profile.
/// </summary>
public sealed record UpdateSystemProfileRequest(
    string? Department,
    string? JobTitle,
    string? Notes
);
