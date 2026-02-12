using HRM.Modules.Identity.Domain.Entities;

namespace HRM.Modules.Identity.Domain.Repositories;

/// <summary>
/// Repository interface for EmployeeProfile entity.
/// </summary>
public interface IEmployeeProfileRepository
{
    Task<EmployeeProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<EmployeeProfile?> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default);

    Task<EmployeeProfile?> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default);

    void Add(EmployeeProfile profile);

    void Update(EmployeeProfile profile);
}
