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
    /// Get the country ID the employee belongs to (from Employee.CountryId).
    /// Returns 0 or 1 item.
    /// </summary>
    Task<IReadOnlyCollection<Guid>> GetEmployeeCountryIdsAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the region ID the employee belongs to (from Employee.RegionId).
    /// Returns 0 or 1 item.
    /// </summary>
    Task<IReadOnlyCollection<Guid>> GetEmployeeRegionIdsAsync(
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
    /// Country ID (0 or 1 item) from Employee.CountryId.
    /// </summary>
    public IReadOnlyCollection<Guid> CountryIds { get; init; } = Array.Empty<Guid>();

    /// <summary>
    /// Region ID (0 or 1 item) from Employee.RegionId.
    /// </summary>
    public IReadOnlyCollection<Guid> RegionIds { get; init; } = Array.Empty<Guid>();

    /// <summary>
    /// Empty dimension IDs.
    /// </summary>
    public static ScopeDimensionIds Empty => new()
    {
        CompanyIds = Array.Empty<Guid>(),
        DepartmentIds = Array.Empty<Guid>(),
        PositionIds = Array.Empty<Guid>(),
        CountryIds = Array.Empty<Guid>(),
        RegionIds = Array.Empty<Guid>()
    };
}
