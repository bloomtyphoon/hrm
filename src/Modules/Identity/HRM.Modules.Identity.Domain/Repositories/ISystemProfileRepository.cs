using HRM.Modules.Identity.Domain.Entities;

namespace HRM.Modules.Identity.Domain.Repositories;

/// <summary>
/// Repository interface for SystemProfile entity.
/// </summary>
public interface ISystemProfileRepository
{
    Task<SystemProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SystemProfile?> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default);

    void Add(SystemProfile profile);

    void Update(SystemProfile profile);
}
