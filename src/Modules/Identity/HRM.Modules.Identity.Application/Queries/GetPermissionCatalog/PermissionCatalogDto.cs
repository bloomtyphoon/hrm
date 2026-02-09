using HRM.BuildingBlocks.Domain.Abstractions.Security;

namespace HRM.Modules.Identity.Application.Queries.GetPermissionCatalog;

/// <summary>
/// DTO representing the full permission catalog for UI selection.
/// </summary>
public sealed record PermissionCatalogDto
{
    public required List<PermissionModuleDto> Modules { get; init; }
}

public sealed record PermissionModuleDto
{
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public required List<PermissionEntityDto> Entities { get; init; }
}

public sealed record PermissionEntityDto
{
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public required List<PermissionActionDto> Actions { get; init; }
}

public sealed record PermissionActionDto
{
    public required string Name { get; init; }
    public required string DisplayName { get; init; }
    public required List<PermissionScopeDto> Scopes { get; init; }
}

public sealed record PermissionScopeDto
{
    public required DataScopeLevel Value { get; init; }
    public required string DisplayName { get; init; }
}
