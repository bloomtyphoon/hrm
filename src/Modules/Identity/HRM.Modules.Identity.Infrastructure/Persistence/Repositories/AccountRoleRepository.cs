using HRM.Modules.Identity.Domain.Entities;
using HRM.Modules.Identity.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Identity.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for AccountRole join entity.
/// </summary>
internal sealed class AccountRoleRepository : IAccountRoleRepository
{
    private readonly IdentityDbContext _context;

    public AccountRoleRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<List<AccountRole>> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await _context.AccountRoles
            .Where(ar => ar.AccountId == accountId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid accountId, Guid roleId, CancellationToken cancellationToken = default)
    {
        return await _context.AccountRoles
            .AsNoTracking()
            .AnyAsync(ar => ar.AccountId == accountId && ar.RoleId == roleId, cancellationToken);
    }

    public async Task<AccountRole?> GetAsync(Guid accountId, Guid roleId, CancellationToken cancellationToken = default)
    {
        return await _context.AccountRoles
            .FirstOrDefaultAsync(ar => ar.AccountId == accountId && ar.RoleId == roleId, cancellationToken);
    }

    public void Add(AccountRole accountRole)
    {
        _context.AccountRoles.Add(accountRole);
    }

    public void Remove(AccountRole accountRole)
    {
        _context.AccountRoles.Remove(accountRole);
    }
}
