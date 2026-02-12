using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Domain.Abstractions.Results;
using HRM.BuildingBlocks.Domain.Abstractions.Security;

namespace HRM.Modules.Identity.Application.Queries.GetEmployeeProfile;

/// <summary>
/// Query to retrieve an employee profile by account ID.
/// </summary>
public sealed record GetEmployeeProfileQuery(Guid AccountId) : IQuery<Result<EmployeeProfileDto>>;

public sealed record EmployeeProfileDto
{
    public Guid Id { get; init; }
    public Guid AccountId { get; init; }
    public Guid EmployeeId { get; init; }
    public DataScopeLevel DefaultScopeLevel { get; init; }
    public Guid? PrimaryCompanyId { get; init; }
    public Guid? PrimaryDepartmentId { get; init; }
    public Guid? PrimaryPositionId { get; init; }
    public bool CanAccessAllAssignedCompanies { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? ModifiedAtUtc { get; init; }
}
