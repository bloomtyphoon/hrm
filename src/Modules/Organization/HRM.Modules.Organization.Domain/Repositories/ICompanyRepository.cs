using HRM.Modules.Organization.Domain.Entities;

namespace HRM.Modules.Organization.Domain.Repositories;

/// <summary>
/// Repository interface for Company aggregate.
/// Defines data access contract - implemented in Infrastructure layer.
///
/// Design Patterns:
/// - Repository Pattern: Abstracts data access from domain
/// - Aggregate Root: Only repository for Company aggregate
/// - Unit of Work: Add/Update/Remove don't save immediately (use DbContext.SaveChangesAsync)
///
/// Implementation Notes:
/// - All async methods for scalability
/// - CancellationToken support for long-running queries
/// - No IQueryable exposure (keeps domain pure)
/// - Returns null for not found (use Result pattern in Application layer)
/// </summary>
public interface ICompanyRepository
{
    /// <summary>
    /// Get company by ID.
    /// Returns null if not found.
    /// </summary>
    /// <param name="id">Company ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Company entity or null if not found</returns>
    Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get company by code.
    /// Returns null if not found.
    /// </summary>
    /// <param name="code">Company code (case-insensitive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Company entity or null if not found</returns>
    Task<Company?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all companies.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all companies</returns>
    Task<IReadOnlyList<Company>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get companies by IDs (batch load, avoids N+1).
    /// </summary>
    Task<IReadOnlyList<Company>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active companies.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of active companies</returns>
    Task<IReadOnlyList<Company>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if company code already exists.
    /// More efficient than GetByCodeAsync when you only need existence check.
    /// </summary>
    /// <param name="code">Company code to check (case-insensitive)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if code exists, false otherwise</returns>
    Task<bool> ExistsByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add new company to repository.
    /// Does NOT save to database immediately (use UnitOfWork.SaveChangesAsync).
    /// </summary>
    /// <param name="company">Company entity to add</param>
    void Add(Company company);

    /// <summary>
    /// Update existing company.
    /// Does NOT save to database immediately (use UnitOfWork.SaveChangesAsync).
    /// </summary>
    /// <param name="company">Company entity to update</param>
    void Update(Company company);

    /// <summary>
    /// Remove company from repository.
    /// Does NOT save to database immediately (use UnitOfWork.SaveChangesAsync).
    /// </summary>
    /// <param name="company">Company entity to remove</param>
    void Remove(Company company);
}
