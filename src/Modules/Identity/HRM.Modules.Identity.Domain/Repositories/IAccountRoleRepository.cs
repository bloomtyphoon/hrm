using HRM.Modules.Identity.Domain.Entities;

namespace HRM.Modules.Identity.Domain.Repositories;

/// <summary>
/// Repository interface for managing Account-Role assignments.
/// </summary>
public interface IAccountRoleRepository
{
    Task<List<AccountRole>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid accountId, Guid roleId, CancellationToken cancellationToken = default);

    void Add(AccountRole accountRole);

    void Remove(AccountRole accountRole);

    Task<AccountRole?> GetAsync(Guid accountId, Guid roleId, CancellationToken cancellationToken = default);
}
