namespace HRM.BuildingBlocks.Application.Abstractions.Personnel;

/// <summary>
/// Cross-module query interface for Personnel data.
///
/// DESIGN: This interface lives in BuildingBlocks as a CONTRACT.
/// - Personnel module IMPLEMENTS this interface
/// - Organization module (and others) CONSUME this interface
/// - No direct dependency between modules
///
/// Usage scenarios:
/// - Organization needs to check which companies an employee is assigned to
///   (for data scope filtering without querying Personnel module directly)
///
/// Follows the same pattern as IOrganizationQuery (consumed by Personnel).
/// </summary>
public interface IPersonnelQuery
{
    /// <summary>
    /// Get all company IDs that an employee has active assignments to.
    /// Returns an empty list if the employee has no assignments.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetEmployeeCompanyIdsAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default);
}
