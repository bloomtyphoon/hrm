namespace HRM.Modules.Personnel.Api.Contracts;

/// <summary>
/// Response DTO for assignment operations.
/// </summary>
public sealed record AssignmentResponse(
    Guid Id,
    Guid EmployeeId,
    Guid CompanyId,
    Guid DepartmentId,
    Guid PositionId,
    DateOnly StartDate,
    DateOnly? EndDate,
    bool IsPrimary,
    string Status
);

/// <summary>
/// Request DTO for adding an assignment to an employee.
/// </summary>
public sealed record AddAssignmentRequest(
    Guid CompanyId,
    Guid DepartmentId,
    Guid PositionId,
    DateOnly StartDate,
    bool IsPrimary = false
);
