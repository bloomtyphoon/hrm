using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Personnel.Application.Commands.CreateEmployee;

/// <summary>
/// Command to create a new employee.
/// </summary>
public sealed record CreateEmployeeCommand(
    string EmployeeCode,
    string FirstName,
    string LastName,
    string Email,
    DateOnly HireDate,
    string? Phone = null,
    DateOnly? DateOfBirth = null,
    Guid? ManagerId = null
) : IModuleCommand<Guid>
{
    public string ModuleName => "Personnel";
}
