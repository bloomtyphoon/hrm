using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Personnel.Application.Commands.AssignManager;

public sealed record AssignManagerCommand(
    Guid EmployeeId,
    Guid ManagerId
) : IModuleCommand
{
    public string ModuleName => "Personnel";
}
