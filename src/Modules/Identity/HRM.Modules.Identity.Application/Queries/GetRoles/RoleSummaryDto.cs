namespace HRM.Modules.Identity.Application.Queries.GetRoles;

/// <summary>
/// Lightweight DTO for role list view.
/// </summary>
public sealed record RoleSummaryDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required bool IsSystemRole { get; init; }
    public required int PermissionCount { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
    public DateTime? ModifiedAtUtc { get; init; }
}
