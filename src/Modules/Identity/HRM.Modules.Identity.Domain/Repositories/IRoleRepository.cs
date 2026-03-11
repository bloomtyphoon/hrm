using HRM.Modules.Identity.Domain.Entities;

namespace HRM.Modules.Identity.Domain.Repositories;

/// <summary>
/// Repository interface for Role aggregate.
/// </summary>
public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<Role>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if role with same name exists within the same company scope.
    /// CompanyId = null checks global roles only.
    /// </summary>
    Task<bool> ExistsByNameAsync(string name, Guid? companyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if role with same name exists within the same company scope, excluding a specific role.
    /// Used for update scenarios.
    /// </summary>
    Task<bool> ExistsByNameAsync(string name, Guid? companyId, Guid excludeId, CancellationToken cancellationToken = default);

    void Add(Role role);

    void Update(Role role);

    void Remove(Role role);
}
