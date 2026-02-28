using HRM.BuildingBlocks.Application.Abstractions.Multitenancy;
using HRM.BuildingBlocks.Domain.Abstractions.Multitenancy;
using HRM.Modules.Personnel.Application.Abstractions;
using HRM.Modules.Personnel.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Personnel.Infrastructure.Persistence.Repositories;

internal sealed class EmployeeRepository : IEmployeeRepository
{
    private readonly PersonnelDbContext _context;
    private readonly ITenantContext? _tenantContext;

    public EmployeeRepository(PersonnelDbContext context, ITenantContext? tenantContext = null)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<Employee?> GetByCodeAsync(string employeeCode, CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .FirstOrDefaultAsync(
                e => e.EmployeeCode.ToLower() == employeeCode.ToLower(),
                cancellationToken);
    }

    public async Task<Employee?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .FirstOrDefaultAsync(
                e => e.Email.ToLower() == email.ToLower(),
                cancellationToken);
    }

    public async Task<Employee?> GetWithAssignmentsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .Include("_assignments")
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Employee>> GetDirectReportsAsync(Guid managerId, CancellationToken cancellationToken = default)
    {
        return await _context.Employees
            .Where(e => e.ManagerId == managerId)
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetAllSubordinateIdsAsync(Guid managerId, CancellationToken cancellationToken = default)
    {
        // Recursive CTE via raw SQL for performance.
        // EF Core cannot wrap CTEs in a subquery (SQL Server limitation), so the global
        // query filter is NOT applied automatically. TenantId must be filtered explicitly.
        var tenantId = _tenantContext?.TenantId;
        var isSystemOrBackground = tenantId is null || tenantId == WellKnownTenants.SystemTenantId;

        List<Guid> ids;

        if (isSystemOrBackground)
        {
            // Background services and system admin see all tenants (consistent with global filter).
            var sql = @"
                WITH Subordinates AS (
                    SELECT Id FROM Personnel.Employees WHERE ManagerId = {0}
                    UNION ALL
                    SELECT e.Id FROM Personnel.Employees e
                    INNER JOIN Subordinates s ON e.ManagerId = s.Id
                )
                SELECT Id FROM Subordinates";

            ids = await _context.Database
                .SqlQueryRaw<Guid>(sql, managerId)
                .ToListAsync(cancellationToken);
        }
        else
        {
            // Customer tenant: restrict CTE traversal to the current tenant only.
            var sql = @"
                WITH Subordinates AS (
                    SELECT Id FROM Personnel.Employees WHERE ManagerId = {0} AND TenantId = {1}
                    UNION ALL
                    SELECT e.Id FROM Personnel.Employees e
                    INNER JOIN Subordinates s ON e.ManagerId = s.Id
                    WHERE e.TenantId = {1}
                )
                SELECT Id FROM Subordinates";

            ids = await _context.Database
                .SqlQueryRaw<Guid>(sql, managerId, tenantId!)
                .ToListAsync(cancellationToken);
        }

        // Include the manager themselves
        ids.Insert(0, managerId);
        return ids;
    }

    public async Task<IReadOnlyList<Guid>> GetManagementChainAsync(Guid employeeId, CancellationToken cancellationToken = default)
    {
        var chain = new List<Guid>();
        var current = await GetByIdAsync(employeeId, cancellationToken);

        while (current != null)
        {
            chain.Add(current.Id);
            if (current.ManagerId.HasValue)
                current = await GetByIdAsync(current.ManagerId.Value, cancellationToken);
            else
                break;
        }

        return chain;
    }

    public async Task<bool> IsSubordinateOfAsync(Guid employeeId, Guid managerId, CancellationToken cancellationToken = default)
    {
        if (employeeId == managerId) return false;

        var subordinateIds = await GetAllSubordinateIdsAsync(managerId, cancellationToken);
        return subordinateIds.Contains(employeeId);
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
