using HRM.BuildingBlocks.Application.Abstractions.Authorization;

namespace HRM.Modules.Personnel.Application.Abstractions;

/// <summary>
/// Query interface for employee assignments.
/// Used for scope resolution — getting employee's companies, departments, positions.
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
    /// Get all scope dimension IDs for an employee in a single query.
    /// </summary>
    Task<ScopeDimensionIds> GetScopeDimensionIdsAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default);
}
