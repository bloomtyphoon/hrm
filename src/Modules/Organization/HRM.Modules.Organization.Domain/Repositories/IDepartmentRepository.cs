using HRM.Modules.Organization.Domain.Entities;

namespace HRM.Modules.Organization.Domain.Repositories;

/// <summary>
/// Repository interface for Department aggregate.
/// </summary>
public interface IDepartmentRepository
{
    Task<Department?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Department>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<bool> ExistsByCodeInCompanyAsync(Guid companyId, string code, CancellationToken cancellationToken = default);

    void Add(Department department);

    void Update(Department department);

    void Remove(Department department);
}
