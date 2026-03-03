using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Attendance.Application.Commands.CheckIn;

/// <summary>
/// Check in the current authenticated employee.
/// EmployeeId is resolved from the current user's execution context (DataScopeRule.SelfEmployeeId).
/// </summary>
public sealed record CheckInCommand(
    DateTime? CheckInTimeUtc = null,   // null = DateTime.UtcNow in handler
    string? Notes = null
) : IModuleCommand<Guid>
{
    public string ModuleName => "Attendance";
}
