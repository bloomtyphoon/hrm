using HRM.Modules.Organization.Domain.Entities;
using HRM.Modules.Organization.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Organization.Infrastructure.Persistence.Repositories;

internal sealed class TenantRepository : ITenantRepository
{
    private readonly OrganizationDbContext _context;

    public TenantRepository(OrganizationDbContext context)
    {
        _context = context;
    }

    public async Task<Tenant?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<Tenant?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .FirstOrDefaultAsync(
                t => t.Code.ToLower() == code.ToLower(),
                cancellationToken);
    }

    public async Task<IReadOnlyList<Tenant>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .OrderBy(t => t.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Tenants
            .AsNoTracking()
            .AnyAsync(
                t => t.Code.ToLower() == code.ToLower(),
                cancellationToken);
    }

    public async Task<Tenant?> GetBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default)
    {
        var normalized = subdomain.ToLowerInvariant();
        return await _context.Tenants
            .FirstOrDefaultAsync(t => t.Subdomain == normalized, cancellationToken);
    }

    public async Task<bool> ExistsBySubdomainAsync(string subdomain, CancellationToken cancellationToken = default)
    {
        var normalized = subdomain.ToLowerInvariant();
        return await _context.Tenants
            .AsNoTracking()
            .AnyAsync(t => t.Subdomain == normalized, cancellationToken);
    }

    public void Add(Tenant tenant)
    {
        _context.Tenants.Add(tenant);
    }

    public void Update(Tenant tenant)
    {
        _context.Tenants.Update(tenant);
    }
}
