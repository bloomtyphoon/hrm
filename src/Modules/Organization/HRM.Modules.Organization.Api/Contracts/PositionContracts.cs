namespace HRM.Modules.Organization.Api.Contracts;

/// <summary>
/// Response DTO for position operations.
/// </summary>
public sealed record PositionResponse(
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

/// <summary>
/// Request DTO for creating a position.
/// </summary>
public sealed record CreatePositionRequest(
    Guid CompanyId,
    string Code,
    string Title,
    int PositionLevel,
    bool IsManagement = false,
    Guid? DepartmentId = null,
    string? Description = null,
    int? MaxHeadcount = null
);
