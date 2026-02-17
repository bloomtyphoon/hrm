using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Organization.Application.Commands.CreatePosition;

/// <summary>
/// Command to create a new position within a company.
/// Optionally linked to a department.
/// </summary>
public sealed record CreatePositionCommand(
    Guid CompanyId,
    string Code,
    string Title,
    int PositionLevel,
    bool IsManagement = false,
    Guid? DepartmentId = null,
    string? Description = null,
    int? MaxHeadcount = null
) : IModuleCommand<Guid>
{
    public string ModuleName => "Organization";
}
