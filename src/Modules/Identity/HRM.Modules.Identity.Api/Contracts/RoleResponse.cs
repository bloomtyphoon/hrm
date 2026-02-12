using HRM.BuildingBlocks.Domain.Abstractions.Security;

namespace HRM.Modules.Identity.Api.Contracts;

/// <summary>
/// Response DTO for role operations.
/// </summary>
public sealed record RoleResponse(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystemRole,
    Guid? CompanyId,
    int PermissionCount,
    List<RolePermissionResponse> Permissions,
    DateTime CreatedAtUtc,
    DateTime? ModifiedAtUtc
);

public sealed record RolePermissionResponse(
    string Module,
    string Entity,
    string Action,
    DataScopeLevel? Scope,
    string PermissionKey
);
