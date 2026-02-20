using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Security;

namespace HRM.Modules.Identity.Application.Commands.CreateRole;

/// <summary>
/// Command to create a new role with permissions.
/// Returns: Role ID on success.
/// </summary>
public sealed record CreateRoleCommand(
    string Name,
    string? Description,
    bool IsSystemRole,
    Guid? CompanyId,
    List<PermissionDto> Permissions
) : IModuleCommand<Guid>
{
    public string ModuleName => "Identity";
}

/// <summary>
/// DTO for permission input in role commands.
/// </summary>
public sealed record PermissionDto(
    string Module,
    string Entity,
    string Action,
    DataScopeLevel? Scope = null
);
