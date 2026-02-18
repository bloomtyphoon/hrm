using HRM.Modules.Personnel.Domain.Entities;

namespace HRM.Modules.Personnel.Application.Queries.GetEmployeeById;

/// <summary>
/// Full detail DTO for a single employee.
/// </summary>
public sealed record EmployeeDetailDto
{
    public required Guid Id { get; init; }
    public required string EmployeeCode { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string FullName { get; init; }
    public required string Email { get; init; }
    public string? Phone { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public required DateOnly HireDate { get; init; }
    public DateOnly? TerminationDate { get; init; }
    public required EmploymentStatus Status { get; init; }
    public Guid? ManagerId { get; init; }
    public Guid? PrimaryCompanyId { get; init; }
    public Guid? PrimaryDepartmentId { get; init; }
    public Guid? PrimaryPositionId { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
    public DateTime? ModifiedAtUtc { get; init; }
}
