using HRM.BuildingBlocks.Domain.Abstractions.Security;

namespace HRM.Modules.Identity.Api.Contracts;

/// <summary>
/// Request DTO for creating an employee profile.
/// </summary>
public sealed record CreateEmployeeProfileRequest(
    Guid EmployeeId,
    DataScopeLevel DefaultScopeLevel = DataScopeLevel.Self,
    Guid? PrimaryCompanyId = null,
    Guid? PrimaryDepartmentId = null,
    Guid? PrimaryPositionId = null,
    IReadOnlyList<Guid>? CompanyIds = null
);
