namespace HRM.Modules.Organization.Application.DTOs;

public sealed record DepartmentDto(
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
