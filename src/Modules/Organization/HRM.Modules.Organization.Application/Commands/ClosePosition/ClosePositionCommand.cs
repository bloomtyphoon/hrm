using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Organization.Application.Commands.ClosePosition;

public sealed record ClosePositionCommand(Guid PositionId) : IModuleCommand
{
    public string ModuleName => "Organization";
}
