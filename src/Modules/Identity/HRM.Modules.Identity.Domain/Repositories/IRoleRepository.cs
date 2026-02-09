using HRM.Modules.Identity.Domain.Entities;

namespace HRM.Modules.Identity.Domain.Repositories;

/// <summary>
/// Repository interface for Role aggregate.
/// </summary>
public interface IRoleRepository
{
    Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(string name, Guid excludeId, CancellationToken cancellationToken = default);

    void Add(Role role);

    void Update(Role role);

    void Remove(Role role);
}
