using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Organization.Application.Commands.DeactivatePosition;

public sealed record DeactivatePositionCommand(Guid PositionId) : IModuleCommand
{
    public string ModuleName => "Organization";
}
