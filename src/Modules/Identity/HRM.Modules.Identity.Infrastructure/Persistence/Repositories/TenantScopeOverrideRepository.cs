using HRM.Modules.Identity.Domain.Entities;
using HRM.Modules.Identity.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Identity.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for TenantScopeOverride.
/// Tenant filtering is handled by ModuleDbContext global query filter.
/// </summary>
internal sealed class TenantScopeOverrideRepository : ITenantScopeOverrideRepository
{
    private readonly IdentityDbContext _context;

    public TenantScopeOverrideRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<TenantScopeOverride?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.TenantScopeOverrides
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<TenantScopeOverride?> GetByPermissionAsync(
        string module, string entity, string action,
        CancellationToken cancellationToken = default)
    {
        return await _context.TenantScopeOverrides
            .FirstOrDefaultAsync(x =>
                x.Module == module &&
                x.Entity == entity &&
                x.Action == action,
                cancellationToken);
    }

    public async Task<List<TenantScopeOverride>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.TenantScopeOverrides
            .AsNoTracking()
            .OrderBy(x => x.Module)
            .ThenBy(x => x.Entity)
            .ThenBy(x => x.Action)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(
        string module, string entity, string action,
        CancellationToken cancellationToken = default)
    {
        return await _context.TenantScopeOverrides
            .AsNoTracking()
            .AnyAsync(x =>
                x.Module == module &&
                x.Entity == entity &&
                x.Action == action,
                cancellationToken);
    }

    public void Add(TenantScopeOverride entity)
    {
        _context.TenantScopeOverrides.Add(entity);
    }

    public void Remove(TenantScopeOverride entity)
    {
        _context.TenantScopeOverrides.Remove(entity);
    }
}
