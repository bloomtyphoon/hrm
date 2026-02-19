using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Personnel.Application.Commands.RemoveManager;

public sealed record RemoveManagerCommand(
    Guid EmployeeId
) : IModuleCommand
{
    public string ModuleName => "Personnel";
}
