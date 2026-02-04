using HRM.Modules.Identity.Domain.Enums;

namespace HRM.Modules.Identity.Application.Queries.GetAccounts;

/// <summary>
/// Lightweight DTO for account list view.
/// </summary>
public sealed record AccountSummaryDto
{
    public required Guid Id { get; init; }
    public required string Username { get; init; }
    public required string Email { get; init; }
    public required string FullName { get; init; }
    public required AccountStatus Status { get; init; }
    public required AccountType AccountType { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
    public DateTime? LastLoginAtUtc { get; init; }
}
