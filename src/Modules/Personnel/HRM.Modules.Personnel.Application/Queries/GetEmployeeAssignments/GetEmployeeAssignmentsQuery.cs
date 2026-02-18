using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.Modules.Personnel.Domain.Entities;

namespace HRM.Modules.Personnel.Application.Queries.GetEmployeeAssignments;

/// <summary>
/// Query to retrieve all assignments for an employee.
/// </summary>
public sealed record GetEmployeeAssignmentsQuery : IQuery<List<AssignmentDto>>
{
    public required Guid EmployeeId { get; init; }
    public AssignmentStatus? Status { get; init; }
}
