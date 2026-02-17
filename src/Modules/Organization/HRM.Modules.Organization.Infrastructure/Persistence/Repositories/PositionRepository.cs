using HRM.Modules.Organization.Domain.Entities;
using HRM.Modules.Organization.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Organization.Infrastructure.Persistence.Repositories;

internal sealed class PositionRepository : IPositionRepository
{
    private readonly OrganizationDbContext _context;

    public PositionRepository(OrganizationDbContext context)
    {
        _context = context;
    }

    public async Task<Position?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Positions
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Position>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await _context.Positions
            .Where(p => p.CompanyId == companyId)
            .OrderBy(p => p.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Position>> GetByDepartmentIdAsync(Guid departmentId, CancellationToken cancellationToken = default)
    {
        return await _context.Positions
            .Where(p => p.DepartmentId == departmentId)
            .OrderBy(p => p.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByCodeInCompanyAsync(Guid companyId, string code, CancellationToken cancellationToken = default)
    {
        return await _context.Positions
            .AsNoTracking()
            .AnyAsync(
                p => p.CompanyId == companyId && p.Code.ToLower() == code.ToLower(),
                cancellationToken
            );
    }

    public void Add(Position position)
    {
        _context.Positions.Add(position);
    }

    public void Update(Position position)
    {
        _context.Positions.Update(position);
    }

    public void Remove(Position position)
    {
        _context.Positions.Remove(position);
    }
}
