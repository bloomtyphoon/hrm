namespace HRM.Modules.Identity.Api.Contracts;

/// <summary>
/// Response DTO for account operations.
/// </summary>
public sealed record AccountResponse(
    Guid Id,
    string Username,
    string Email,
    string FullName,
    string? PhoneNumber,
    string Status,
    string AccountType,
    bool IsTwoFactorEnabled,
    DateTime? ActivatedAtUtc,
    DateTime? LastLoginAtUtc,
    DateTime CreatedAtUtc,
    DateTime? ModifiedAtUtc
);
