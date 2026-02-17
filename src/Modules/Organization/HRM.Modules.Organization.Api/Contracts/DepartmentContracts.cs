namespace HRM.Modules.Organization.Api.Contracts;

/// <summary>
/// Response DTO for department operations.
/// </summary>
public sealed record DepartmentResponse(
    Guid Id,
    string Code,
    string Name,
    Guid CompanyId,
    Guid? ParentDepartmentId,
    Guid? ManagerId,
    int Level,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? ModifiedAtUtc
);

/// <summary>
/// Request DTO for creating a department.
/// </summary>
public sealed record CreateDepartmentRequest(
    Guid CompanyId,
    string Code,
    string Name,
    Guid? ParentDepartmentId = null,
    Guid? ManagerId = null
);
