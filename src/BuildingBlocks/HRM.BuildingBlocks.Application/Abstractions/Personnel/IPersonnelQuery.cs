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

    /// <summary>
    /// Get employee's organizational info needed for approval chain resolution.
    /// Returns null if employee not found.
    /// </summary>
    Task<EmployeeApprovalInfo?> GetEmployeeApprovalInfoAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Lightweight DTO for approval chain resolution.
/// </summary>
public sealed record EmployeeApprovalInfo(
    Guid EmployeeId,
    Guid? ManagerId,
    Guid? PrimaryDepartmentId,
    Guid? PrimaryCompanyId);
