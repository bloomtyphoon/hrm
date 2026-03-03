using HRM.Modules.Personnel.Domain.Entities;

namespace HRM.Modules.Personnel.Application.Abstractions;

/// <summary>
/// Repository interface for Employee aggregate.
/// </summary>
public interface IEmployeeRepository
{
    /// <summary>
    /// Get employee by ID.
    /// </summary>
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get employee by employee code.
    /// </summary>
    Task<Employee?> GetByCodeAsync(string employeeCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get employee by email.
    /// </summary>
    Task<Employee?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get employee with all assignments.
    /// </summary>
    Task<Employee?> GetWithAssignmentsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all direct reports of a manager.
    /// </summary>
    Task<IReadOnlyList<Employee>> GetDirectReportsAsync(Guid managerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all subordinate IDs (recursive) of a manager.
    /// Includes the manager themselves (self-inclusion semantics).
    /// Uses the closure table for O(1) lookup.
    /// </summary>
    Task<IReadOnlySet<Guid>> GetAllSubordinateIdsAsync(Guid managerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the management chain for an employee (bottom-up).
    /// Returns [employee, directManager, manager's manager, ...] ordered from self to root.
    /// Uses the closure table — single query, no N+1.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetManagementChainAsync(Guid employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if an employee is a subordinate of a manager.
    /// Uses a direct closure table EXISTS query — O(1), no subtree load.
    /// </summary>
    Task<bool> IsSubordinateOfAsync(Guid employeeId, Guid managerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rebuild the entire closure table for a tenant from scratch.
    /// Use after bulk org-chart import or HR system sync.
    ///
    /// Steps:
    ///   1. Delete all existing closure rows for the tenant.
    ///   2. Insert self-reference rows for all active employees.
    ///   3. Rebuild all ancestor-descendant paths using recursive CTE.
    /// </summary>
    Task RebuildHierarchyAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a new employee.
    /// </summary>
    void Add(Employee employee);

    /// <summary>
    /// Update an employee.
    /// </summary>
    void Update(Employee employee);

    /// <summary>
    /// Delete an employee.
    /// </summary>
    void Delete(Employee employee);
}
