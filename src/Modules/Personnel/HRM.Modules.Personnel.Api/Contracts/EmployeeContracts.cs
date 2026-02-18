namespace HRM.Modules.Personnel.Api.Contracts;

/// <summary>
/// Response DTO for employee operations.
/// </summary>
public sealed record EmployeeResponse(
    Guid Id,
    string EmployeeCode,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string? Phone,
    DateOnly? DateOfBirth,
    DateOnly HireDate,
    DateOnly? TerminationDate,
    string Status,
    Guid? ManagerId,
    Guid? PrimaryCompanyId,
    Guid? PrimaryDepartmentId,
    Guid? PrimaryPositionId,
    DateTime CreatedAtUtc,
    DateTime? ModifiedAtUtc
);

/// <summary>
/// Request DTO for creating an employee.
/// </summary>
public sealed record CreateEmployeeRequest(
    string EmployeeCode,
    string FirstName,
    string LastName,
    string Email,
    DateOnly HireDate,
    string? Phone = null,
    DateOnly? DateOfBirth = null,
    Guid? ManagerId = null
);
