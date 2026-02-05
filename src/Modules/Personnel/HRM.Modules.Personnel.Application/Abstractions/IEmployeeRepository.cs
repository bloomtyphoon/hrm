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
    /// Get all subordinates (recursive) of a manager.
    /// Includes the manager themselves.
    /// </summary>
    Task<IReadOnlyList<Guid>> GetAllSubordinateIdsAsync(Guid managerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the management chain for an employee (bottom-up).
    /// </summary>
    Task<IReadOnlyList<Guid>> GetManagementChainAsync(Guid employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if an employee is a subordinate of a manager.
    /// </summary>
    Task<bool> IsSubordinateOfAsync(Guid employeeId, Guid managerId, CancellationToken cancellationToken = default);

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
