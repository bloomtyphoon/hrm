using HRM.Modules.Organization.Domain.Entities;
using HRM.Modules.Organization.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Organization.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Company aggregate.
/// Provides data access methods using EF Core.
///
/// Design Patterns:
/// - Repository Pattern: Encapsulates data access logic
/// - Unit of Work: DbContext tracks changes, SaveChangesAsync commits
/// - Query Object: Returns entities, not IQueryable (keeps domain pure)
///
/// Performance Optimizations:
/// - AsNoTracking for existence checks (faster, less memory)
/// - Indexed columns for Code queries (see OrganizationDbContext)
/// - Case-insensitive comparisons via SQL collation
/// </summary>
internal sealed class CompanyRepository : ICompanyRepository
{
    private readonly OrganizationDbContext _context;

    public CompanyRepository(OrganizationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get company by ID.
    /// Returns null if not found.
    /// </summary>
    public async Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Companies
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    /// <summary>
    /// Get company by code (case-insensitive).
    /// Returns null if not found.
    /// </summary>
    public async Task<Company?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Companies
            .FirstOrDefaultAsync(c => c.Code == code, cancellationToken);
    }

    /// <summary>
    /// Get companies by IDs (batch load).
    /// </summary>
    public async Task<IReadOnlyList<Company>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        return await _context.Companies
            .Where(c => ids.Contains(c.Id))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get all companies.
    /// </summary>
    public async Task<IReadOnlyList<Company>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Companies
            .OrderBy(c => c.Code)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Get all active companies.
    /// </summary>
    public async Task<IReadOnlyList<Company>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Companies
            .Where(c => c.Status == CompanyStatus.Active)
            .OrderBy(c => c.Code)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Check if company code exists (case-insensitive).
    /// More efficient than GetByCodeAsync for existence checks.
    /// </summary>
    public async Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        return await _context.Companies
            .AsNoTracking()
            .AnyAsync(c => c.Code == code, cancellationToken);
    }

    /// <summary>
    /// Add new company to repository.
    /// Does NOT commit to database (use SaveChangesAsync).
    /// </summary>
    public void Add(Company company)
    {
        _context.Companies.Add(company);
    }

    /// <summary>
    /// Update existing company.
    /// Does NOT commit to database (use SaveChangesAsync).
    /// </summary>
    public void Update(Company company)
    {
        _context.Companies.Update(company);
    }

    /// <summary>
    /// Remove company from repository.
    /// Does NOT commit to database (use SaveChangesAsync).
    /// </summary>
    public void Remove(Company company)
    {
        _context.Companies.Remove(company);
    }
}
