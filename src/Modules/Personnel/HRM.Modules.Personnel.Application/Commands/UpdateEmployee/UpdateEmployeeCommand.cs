using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Personnel.Application.Commands.UpdateEmployee;

public sealed record UpdateEmployeeCommand(
    Guid EmployeeId,
    string FirstName,
    string LastName,
    string Email,
    string? Phone = null,
    DateOnly? DateOfBirth = null
) : IModuleCommand
{
    public string ModuleName => "Personnel";
}
