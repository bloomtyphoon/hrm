using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Organization.Application.Commands.UpdatePosition;

public sealed record UpdatePositionCommand(
    Guid PositionId,
    string Title,
    int PositionLevel,
    bool IsManagement,
    string? Description = null,
    int? MaxHeadcount = null
) : IModuleCommand
{
    public string ModuleName => "Organization";
}
