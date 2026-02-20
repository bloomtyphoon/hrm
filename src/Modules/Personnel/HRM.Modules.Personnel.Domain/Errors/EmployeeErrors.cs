using HRM.BuildingBlocks.Domain.Abstractions.Results;

namespace HRM.Modules.Personnel.Domain.Errors;

/// <summary>
/// Static error definitions for Employee operations.
/// </summary>
public static class EmployeeErrors
{
    public static ConflictError CodeAlreadyExists(string code) =>
        new("Employee.CodeAlreadyExists",
            $"Employee code '{code}' is already in use.");

    public static ConflictError EmailAlreadyExists(string email) =>
        new("Employee.EmailAlreadyExists",
            $"Email '{email}' is already in use.");

    public static NotFoundError NotFound(Guid id) =>
        new("Employee.NotFound",
            $"Employee with ID '{id}' was not found.");

    public static NotFoundError NotFoundByCode(string code) =>
        new("Employee.NotFoundByCode",
            $"Employee with code '{code}' was not found.");

    public static ValidationError AlreadyTerminated(Guid id) =>
        new("Employee.AlreadyTerminated",
            $"Employee '{id}' is already terminated.");

    public static NotFoundError AssignmentNotFound(Guid assignmentId) =>
        new("Employee.AssignmentNotFound",
            $"Assignment with ID '{assignmentId}' was not found.");
}
