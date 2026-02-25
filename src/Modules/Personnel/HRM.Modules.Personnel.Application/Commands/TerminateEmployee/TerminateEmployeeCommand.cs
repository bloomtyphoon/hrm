using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Personnel.Application.Commands.TerminateEmployee;

public sealed record TerminateEmployeeCommand(
    Guid EmployeeId,
    DateOnly TerminationDate
) : IModuleCommand
{
    public string ModuleName => "Personnel";
}
