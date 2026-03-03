using HRM.Modules.Organization.Domain.Entities;

namespace HRM.Modules.Organization.Domain.Repositories;

/// <summary>
/// Repository interface for Tenant aggregate.
/// </summary>
public interface ITenantRepository
{
    Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Tenant?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);

    void Add(Tenant tenant);

    void Update(Tenant tenant);
}
