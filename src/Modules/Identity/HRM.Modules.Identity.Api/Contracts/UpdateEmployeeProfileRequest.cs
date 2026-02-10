using HRM.BuildingBlocks.Domain.Abstractions.Security;

namespace HRM.Modules.Identity.Api.Contracts;

/// <summary>
/// Request DTO for updating an employee profile.
/// </summary>
public sealed record UpdateEmployeeProfileRequest(
    Guid? PrimaryCompanyId,
    Guid? PrimaryDepartmentId,
    Guid? PrimaryPositionId,
    DataScopeLevel DefaultScopeLevel,
    bool CanAccessAllAssignedCompanies
);
