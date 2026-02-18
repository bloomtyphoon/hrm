using HRM.Modules.Organization.Domain.Entities;
using HRM.Modules.Organization.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Organization.Infrastructure.Persistence.Repositories;

internal sealed class DepartmentRepository : IDepartmentRepository
{
    private readonly OrganizationDbContext _context;

    public DepartmentRepository(OrganizationDbContext context)
    {
        _context = context;
    }

    public async Task<Department?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Department>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return await _context.Departments
            .Where(d => d.CompanyId == companyId)
            .OrderBy(d => d.Level)
            .ThenBy(d => d.Code)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsByCodeInCompanyAsync(Guid companyId, string code, CancellationToken cancellationToken = default)
    {
        return await _context.Departments
            .AsNoTracking()
            .AnyAsync(
                d => d.CompanyId == companyId && d.Code.ToLower() == code.ToLower(),
                cancellationToken
            );
    }

    public void Add(Department department)
    {
        _context.Departments.Add(department);
    }

    public void Update(Department department)
    {
        _context.Departments.Update(department);
    }

    public void Remove(Department department)
    {
        _context.Departments.Remove(department);
    }
}
