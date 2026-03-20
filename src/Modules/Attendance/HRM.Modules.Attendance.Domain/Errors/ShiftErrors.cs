using HRM.BuildingBlocks.Domain.Abstractions.Results;

namespace HRM.Modules.Attendance.Domain.Errors;

public static class ShiftErrors
{
    public static NotFoundError ShiftNotFound(Guid id) =>
        new("Shift.NotFound", $"Shift '{id}' was not found.");

    public static NotFoundError AssignmentNotFound(Guid id) =>
        new("ShiftAssignment.NotFound", $"Shift assignment '{id}' was not found.");

    public static ForbiddenError ManageForbidden() =>
        new("Shift.Manage.Forbidden", "You don't have permission to manage shifts.");

    public static ForbiddenError AssignForbidden() =>
        new("ShiftAssignment.Forbidden", "You don't have permission to manage shift assignments.");

    public static ConflictError DuplicateName(string name) =>
        new("Shift.DuplicateName", $"A shift with name '{name}' already exists.");
}
