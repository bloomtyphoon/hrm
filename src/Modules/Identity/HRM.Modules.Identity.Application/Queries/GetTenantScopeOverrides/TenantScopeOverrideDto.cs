using HRM.BuildingBlocks.Domain.Abstractions.Security;

namespace HRM.Modules.Identity.Application.Queries.GetTenantScopeOverrides;

public sealed record TenantScopeOverrideDto
{
    public required Guid Id { get; init; }
    public required string Module { get; init; }
    public required string Entity { get; init; }
    public required string Action { get; init; }
    public required string PermissionKey { get; init; }
    public required List<DataScopeLevel> AllowedScopes { get; init; }
    public required DataScopeLevel? DefaultScope { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
    public required DateTime? ModifiedAtUtc { get; init; }
}
