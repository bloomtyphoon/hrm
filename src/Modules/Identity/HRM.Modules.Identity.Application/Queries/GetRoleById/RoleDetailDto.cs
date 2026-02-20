using HRM.BuildingBlocks.Domain.Abstractions.Security;

namespace HRM.Modules.Identity.Application.Queries.GetRoleById;

/// <summary>
/// Detailed DTO for single role view, including permissions.
/// </summary>
public sealed record RoleDetailDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required bool IsSystemRole { get; init; }
    public Guid? CompanyId { get; init; }
    public required int PermissionCount { get; init; }
    public required List<RolePermissionDto> Permissions { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
    public DateTime? ModifiedAtUtc { get; init; }
}

public sealed record RolePermissionDto
{
    public required string Module { get; init; }
    public required string Entity { get; init; }
    public required string Action { get; init; }
    public DataScopeLevel? Scope { get; init; }
    public required string PermissionKey { get; init; }
}
