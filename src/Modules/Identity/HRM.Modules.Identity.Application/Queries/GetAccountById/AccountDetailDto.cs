using HRM.Modules.Identity.Domain.Enums;

namespace HRM.Modules.Identity.Application.Queries.GetAccountById;

/// <summary>
/// Full DTO for single account detail view.
/// Contains all fields needed by the API response.
/// </summary>
public sealed record AccountDetailDto
{
    public required Guid Id { get; init; }
    public required string Username { get; init; }
    public required string Email { get; init; }
    public required string FullName { get; init; }
    public string? PhoneNumber { get; init; }
    public required AccountStatus Status { get; init; }
    public required AccountType AccountType { get; init; }
    public required bool IsTwoFactorEnabled { get; init; }
    public DateTime? ActivatedAtUtc { get; init; }
    public DateTime? LastLoginAtUtc { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
    public DateTime? ModifiedAtUtc { get; init; }
}
