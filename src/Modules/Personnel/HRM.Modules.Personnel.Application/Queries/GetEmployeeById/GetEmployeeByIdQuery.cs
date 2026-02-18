using HRM.BuildingBlocks.Application.Abstractions.Queries;

namespace HRM.Modules.Personnel.Application.Queries.GetEmployeeById;

/// <summary>
/// Query to retrieve a single employee by ID.
/// Returns null if not found.
/// </summary>
public sealed record GetEmployeeByIdQuery : IQuery<EmployeeDetailDto?>
{
    public required Guid EmployeeId { get; init; }
}
