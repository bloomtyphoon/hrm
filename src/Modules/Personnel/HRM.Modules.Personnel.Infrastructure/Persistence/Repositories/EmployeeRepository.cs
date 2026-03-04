using HRM.Modules.Personnel.Application.Abstractions;
using HRM.Modules.Personnel.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Personnel.Infrastructure.Persistence.Repositories;

internal sealed class EmployeeRepository : IEmployeeRepository
{
    private readonly PersonnelDbContext _context;

    public EmployeeRepository(PersonnelDbContext context)
    {
        _context = context;
    }

    public async Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<Employee?> GetByCodeAsync(string employeeCode, CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .FirstOrDefaultAsync(e => e.EmployeeCode == employeeCode, cancellationToken);
    }

    public async Task<Employee?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .FirstOrDefaultAsync(e => e.Email == email, cancellationToken);
    }

    public async Task<Employee?> GetWithAssignmentsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .Include(e => e.Assignments)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Employee>> GetDirectReportsAsync(
        Guid managerId, CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .Where(e => e.ManagerId == managerId)
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    /// Uses closure table: SELECT DescendantId WHERE AncestorId = managerId.
    /// Depth=0 row (self-reference) naturally includes the manager themselves.
    public async Task<IReadOnlySet<Guid>> GetAllSubordinateIdsAsync(
        Guid managerId, CancellationToken cancellationToken = default)
    {
        var ids = await _context.EmployeeHierarchyClosures
            .Where(c => c.AncestorId == managerId)
            .Select(c => c.DescendantId)
            .ToListAsync(cancellationToken);

        return ids.ToHashSet();
    }

    /// <inheritdoc />
    /// Uses closure table: single query ordered by Depth ASC.
    /// Replaces the previous N+1 loop (one GetByIdAsync per level).
    public async Task<IReadOnlyList<Guid>> GetManagementChainAsync(
        Guid employeeId, CancellationToken cancellationToken = default)
    {
        return await _context.EmployeeHierarchyClosures
            .Where(c => c.DescendantId == employeeId)
            .OrderBy(c => c.Depth)
            .Select(c => c.AncestorId)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    /// Uses closure table EXISTS query — O(1), no subtree load.
    /// Depth > 0 excludes self-reference rows (an employee is not their own subordinate).
    public async Task<bool> IsSubordinateOfAsync(
        Guid employeeId, Guid managerId, CancellationToken cancellationToken = default)
    {
        if (employeeId == managerId) return false;

        return await _context.EmployeeHierarchyClosures
            .AnyAsync(
                c => c.AncestorId   == managerId &&
                     c.DescendantId == employeeId &&
                     c.Depth        > 0,
                cancellationToken);
    }

    /// <inheritdoc />
    /// Full closure table rebuild for a tenant.
    /// Use after bulk import or HR system sync.
    public async Task RebuildHierarchyAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        // Step 1: Remove all existing closure rows for this tenant
        var existing = await _context.EmployeeHierarchyClosures
            .Where(c => c.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        _context.EmployeeHierarchyClosures.RemoveRange(existing);
        await _context.SaveChangesAsync(cancellationToken);

        // Step 2: Insert self-reference rows for all active employees
        var employees = await _context.Employees
            .Where(e => e.TenantId == tenantId)
            .Select(e => new { e.Id, e.TenantId })
            .ToListAsync(cancellationToken);

        var selfRows = employees.Select(e => new EmployeeHierarchyClosure
        {
            TenantId     = e.TenantId,
            AncestorId   = e.Id,
            DescendantId = e.Id,
            Depth        = 0
        }).ToList();

        _context.EmployeeHierarchyClosures.AddRange(selfRows);
        await _context.SaveChangesAsync(cancellationToken);

        // Step 3: Rebuild all ancestor-descendant paths via SQL recursive CTE
        // This is more efficient than doing it in application code for large orgs.
        await _context.Database.ExecuteSqlRawAsync(
            """
            ;WITH Hierarchy AS (
                SELECT
                    e.TenantId,
                    e.ManagerId   AS AncestorId,
                    e.Id          AS DescendantId,
                    1             AS Depth
                FROM Personnel.Employees e
                WHERE e.ManagerId IS NOT NULL
                  AND e.TenantId  = {0}

                UNION ALL

                SELECT
                    h.TenantId,
                    e.ManagerId,
                    h.DescendantId,
                    h.Depth + 1
                FROM Hierarchy h
                INNER JOIN Personnel.Employees e
                    ON e.Id       = h.AncestorId
                    AND e.TenantId = h.TenantId
                WHERE e.ManagerId IS NOT NULL
            )
            INSERT INTO Personnel.EmployeeHierarchyClosures
                (TenantId, AncestorId, DescendantId, Depth)
            SELECT DISTINCT TenantId, AncestorId, DescendantId, Depth
            FROM Hierarchy
            """,
            tenantId);
    }

    public void Add(Employee employee)
    {
        _context.Employees.Add(employee);
    }

    public void Update(Employee employee)
    {
        _context.Employees.Update(employee);
    }

    public void Delete(Employee employee)
    {
        _context.Employees.Remove(employee);
    }
}
