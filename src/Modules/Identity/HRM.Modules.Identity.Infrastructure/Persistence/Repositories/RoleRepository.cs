using HRM.Modules.Identity.Domain.Entities;
using HRM.Modules.Identity.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Identity.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Role aggregate.
/// </summary>
internal sealed class RoleRepository : IRoleRepository
{
    private readonly IdentityDbContext _context;

    public RoleRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .FirstOrDefaultAsync(
                r => r.Name.ToLower() == name.ToLower(),
                cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .AsNoTracking()
            .AnyAsync(
                r => r.Name.ToLower() == name.ToLower(),
                cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(string name, Guid excludeId, CancellationToken cancellationToken = default)
    {
        return await _context.Roles
            .AsNoTracking()
            .AnyAsync(
                r => r.Name.ToLower() == name.ToLower() && r.Id != excludeId,
                cancellationToken);
    }

    public void Add(Role role)
    {
        _context.Roles.Add(role);
    }

    public void Update(Role role)
    {
        _context.Roles.Update(role);
    }

    public void Remove(Role role)
    {
        _context.Roles.Remove(role);
    }
}
