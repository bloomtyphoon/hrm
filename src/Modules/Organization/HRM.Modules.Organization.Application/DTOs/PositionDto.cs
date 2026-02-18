namespace HRM.Modules.Organization.Application.DTOs;

public sealed record PositionDto(
    Guid Id,
    string Code,
    string Title,
    string? Description,
    Guid CompanyId,
    Guid? DepartmentId,
    int PositionLevel,
    bool IsManagement,
    int? MaxHeadcount,
    string Status,
    DateTime CreatedAtUtc,
    DateTime? ModifiedAtUtc
);
