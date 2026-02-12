using HRM.Modules.Identity.Domain.Entities;
using HRM.Modules.Identity.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Identity.Infrastructure.Persistence.Repositories;

internal sealed class EmployeeProfileRepository : IEmployeeProfileRepository
{
    private readonly IdentityDbContext _context;

    public EmployeeProfileRepository(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<EmployeeProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.EmployeeProfiles.FirstOrDefaultAsync(ep => ep.Id == id, cancellationToken);
    }

    public async Task<EmployeeProfile?> GetByAccountIdAsync(Guid accountId, CancellationToken cancellationToken = default)
    {
        return await _context.EmployeeProfiles.FirstOrDefaultAsync(ep => ep.AccountId == accountId, cancellationToken);
    }

    public async Task<EmployeeProfile?> GetByEmployeeIdAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _context.EmployeeProfiles.FirstOrDefaultAsync(ep => ep.EmployeeId == employeeId, cancellationToken);
    }

    public void Add(EmployeeProfile profile)
    {
        _context.EmployeeProfiles.Add(profile);
    }

    public void Update(EmployeeProfile profile)
    {
        _context.EmployeeProfiles.Update(profile);
    }
}
