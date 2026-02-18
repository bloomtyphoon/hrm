using HRM.Modules.Personnel.Domain.Entities;

namespace HRM.Modules.Personnel.Application.Queries.GetEmployeeAssignments;

/// <summary>
/// DTO for employee assignment detail.
/// </summary>
public sealed record AssignmentDto
{
    public required Guid Id { get; init; }
    public required Guid EmployeeId { get; init; }
    public required Guid CompanyId { get; init; }
    public required Guid DepartmentId { get; init; }
    public required Guid PositionId { get; init; }
    public required DateOnly StartDate { get; init; }
    public DateOnly? EndDate { get; init; }
    public required bool IsPrimary { get; init; }
    public required AssignmentStatus Status { get; init; }
}
