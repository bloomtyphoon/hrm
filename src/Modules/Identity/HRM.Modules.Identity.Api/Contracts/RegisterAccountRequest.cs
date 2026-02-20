namespace HRM.Modules.Identity.Api.Contracts;

/// <summary>
/// Request DTO for registering a new account.
/// </summary>
public sealed record RegisterAccountRequest(
    string Username,
    string Email,
    string Password,
    string FullName,
    string? PhoneNumber = null
);
