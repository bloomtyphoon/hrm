namespace HRM.Modules.Identity.Api.Contracts;

/// <summary>
/// Request DTO for creating a system profile.
/// </summary>
public sealed record CreateSystemProfileRequest(
    bool IsSuperAdmin = false,
    string? Department = null,
    string? JobTitle = null
);
