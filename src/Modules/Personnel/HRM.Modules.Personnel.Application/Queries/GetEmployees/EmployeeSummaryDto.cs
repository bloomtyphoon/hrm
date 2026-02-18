using HRM.Modules.Personnel.Domain.Entities;

namespace HRM.Modules.Personnel.Application.Queries.GetEmployees;

/// <summary>
/// Lightweight DTO for employee list view.
/// </summary>
public sealed record EmployeeSummaryDto
{
    public required Guid Id { get; init; }
    public required string EmployeeCode { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string FullName { get; init; }
    public required string Email { get; init; }
    public string? Phone { get; init; }
    public required DateOnly HireDate { get; init; }
    public required EmploymentStatus Status { get; init; }
    public Guid? ManagerId { get; init; }
    public Guid? PrimaryCompanyId { get; init; }
    public Guid? PrimaryDepartmentId { get; init; }
    public Guid? PrimaryPositionId { get; init; }
    public required DateTime CreatedAtUtc { get; init; }
}
