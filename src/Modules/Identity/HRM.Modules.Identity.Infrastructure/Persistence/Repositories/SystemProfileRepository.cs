using HRM.Modules.Identity.Domain.Entities;
using HRM.Modules.Identity.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Identity.Infrastructure.Persistence.Repositories;

internal sealed class SystemProfileRepository : ISystemProfileRepository
{
    private readonly IdentityDbContext _context;

    public SystemProfileRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<SystemProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SystemProfiles.FirstOrDefaultAsync(sp => sp.Id == id, cancellationToken);
    }

    public async Task<SystemProfile?> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await _context.SystemProfiles.FirstOrDefaultAsync(sp => sp.AccountId == accountId, cancellationToken);
    }

    public void Add(SystemProfile profile)
    {
        _context.SystemProfiles.Add(profile);
    }

    public void Update(SystemProfile profile)
    {
        _context.SystemProfiles.Update(profile);
    }
}
