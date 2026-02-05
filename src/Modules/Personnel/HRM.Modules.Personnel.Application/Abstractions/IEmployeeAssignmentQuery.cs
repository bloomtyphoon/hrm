namespace HRM.Modules.Personnel.Application.Abstractions;

/// <summary>
/// Query interface for employee assignments.
/// Used for scope resolution - getting employee's companies, departments, positions.
/// </summary>
public interface IEmployeeAssignmentQuery
{
    /// <summary>
    /// Get all company IDs an employee is assigned to.
    /// </summary>
    Task<IReadOnlyCollection<Guid>> GetEmployeeCompanyIdsAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all department IDs an employee is assigned to.
    /// </summary>
    Task<IReadOnlyCollection<Guid>> GetEmployeeDepartmentIdsAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all position IDs an employee is assigned to.
    /// </summary>
    Task<IReadOnlyCollection<Guid>> GetEmployeePositionIdsAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get scope dimension IDs for an employee at a specific level.
    /// </summary>
    Task<ScopeDimensionIds> GetScopeDimensionIdsAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Aggregated scope dimension IDs for an employee.
/// </summary>
public sealed record ScopeDimensionIds
{
    /// <summary>
    /// All company IDs the employee is assigned to.
    /// </summary>
    public required IReadOnlyCollection<Guid> CompanyIds { get; init; }

    /// <summary>
    /// All department IDs the employee is assigned to.
    /// </summary>
    public required IReadOnlyCollection<Guid> DepartmentIds { get; init; }

    /// <summary>
    /// All position IDs the employee is assigned to.
    /// </summary>
    public required IReadOnlyCollection<Guid> PositionIds { get; init; }

    /// <summary>
    /// Empty dimension IDs.
    /// </summary>
    public static ScopeDimensionIds Empty => new()
    {
        CompanyIds = Array.Empty<Guid>(),
        DepartmentIds = Array.Empty<Guid>(),
        PositionIds = Array.Empty<Guid>()
    };
}
