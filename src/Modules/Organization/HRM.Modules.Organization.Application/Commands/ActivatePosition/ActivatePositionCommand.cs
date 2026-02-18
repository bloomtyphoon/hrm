using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Organization.Application.Commands.ActivatePosition;

public sealed record ActivatePositionCommand(Guid PositionId) : IModuleCommand
{
    public string ModuleName => "Organization";
}
