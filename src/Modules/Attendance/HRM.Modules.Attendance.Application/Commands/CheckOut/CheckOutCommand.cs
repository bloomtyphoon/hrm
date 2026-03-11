using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Attendance.Application.Commands.CheckOut;

/// <summary>
/// Check out the current authenticated employee.
/// EmployeeId is resolved from the current user's execution context.
/// </summary>
public sealed record CheckOutCommand(
    DateTime? CheckOutTimeUtc = null,  // null = DateTime.UtcNow in handler
    string? Notes = null
) : IModuleCommand<Guid>
{
    public string ModuleName => "Attendance";
}
