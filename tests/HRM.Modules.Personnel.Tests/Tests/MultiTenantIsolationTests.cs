using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Domain.Abstractions.Multitenancy;
using HRM.Modules.Personnel.Application.Abstractions;
using HRM.Modules.Personnel.Domain.Entities;
using HRM.Modules.Personnel.Infrastructure.Persistence;
using HRM.Modules.Personnel.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HRM.Modules.Personnel.Tests.Tests;

/// <summary>
/// Tests for multi-tenant data isolation boundary.
///
/// Verifies that:
///   1. A tenant's employees are invisible to another tenant's DbContext
///   2. The system tenant (WellKnownTenants.SystemTenantId) bypasses all filters
///   3. Closure table queries are tenant-scoped — hierarchy of one tenant
///      never bleeds into another tenant's hierarchy lookups
/// </summary>
public class MultiTenantIsolationTests
{
    private static readonly Guid TenantA = Guid.NewGuid();
    private static readonly Guid TenantB = Guid.NewGuid();
    private static readonly DateOnly HireDate = new(2020, 1, 1);

    // ──────────────────────────────────────────────────────────────────
    // Scenario 1: Basic employee visibility isolation
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TenantA_CannotSee_TenantB_Employees()
    {
        // Arrange: shared DB name so both tenant contexts read the same backing store
        var dbName = Guid.NewGuid().ToString();

        // Seed employees for both tenants using the system tenant (bypasses filter for writes)
        using (var seedScope = PersonnelServiceFactory.CreateScope(WellKnownTenants.SystemTenantId, dbName))
        {
            var db   = seedScope.ServiceProvider.GetRequiredService<PersonnelDbContext>();
            var repo = seedScope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

            repo.Add(Employee.Create(TenantA, "A001", "Alice",  "Smith", "alice@a.com",  HireDate));
            repo.Add(Employee.Create(TenantA, "A002", "Bob",    "Jones", "bob@a.com",    HireDate));
            repo.Add(Employee.Create(TenantB, "B001", "Carlos", "Ruiz",  "carlos@b.com", HireDate));
            repo.Add(Employee.Create(TenantB, "B002", "Diana",  "Kim",   "diana@b.com",  HireDate));
            await db.CommitAsync();
        }

        // Act + Assert: TenantA context sees only TenantA employees
        using (var scopeA = PersonnelServiceFactory.CreateScope(TenantA, dbName))
        {
            var dbA = scopeA.ServiceProvider.GetRequiredService<PersonnelDbContext>();
            var employees = await dbA.Employees.ToListAsync();

            Assert.Equal(2, employees.Count);
            Assert.All(employees, e => Assert.Equal(TenantA, e.TenantId));
        }

        // Act + Assert: TenantB context sees only TenantB employees
        using (var scopeB = PersonnelServiceFactory.CreateScope(TenantB, dbName))
        {
            var dbB = scopeB.ServiceProvider.GetRequiredService<PersonnelDbContext>();
            var employees = await dbB.Employees.ToListAsync();

            Assert.Equal(2, employees.Count);
            Assert.All(employees, e => Assert.Equal(TenantB, e.TenantId));
        }
    }

    [Fact]
    public async Task SystemTenant_CanSeeAll_Employees()
    {
        var dbName = Guid.NewGuid().ToString();

        using (var seedScope = PersonnelServiceFactory.CreateScope(WellKnownTenants.SystemTenantId, dbName))
        {
            var db   = seedScope.ServiceProvider.GetRequiredService<PersonnelDbContext>();
            var repo = seedScope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

            repo.Add(Employee.Create(TenantA, "A001", "Alice",  "Smith", "alice@a.com",  HireDate));
            repo.Add(Employee.Create(TenantB, "B001", "Carlos", "Ruiz",  "carlos@b.com", HireDate));
            await db.CommitAsync();
        }

        using var systemScope = PersonnelServiceFactory.CreateScope(WellKnownTenants.SystemTenantId, dbName);
        var sysDb = systemScope.ServiceProvider.GetRequiredService<PersonnelDbContext>();
        var all   = await sysDb.Employees.ToListAsync();

        Assert.Equal(2, all.Count);
    }

    // ──────────────────────────────────────────────────────────────────
    // Scenario 2: GetByIdAsync across tenants
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenEmployeeBelongsToDifferentTenant()
    {
        var dbName = Guid.NewGuid().ToString();
        Guid tenantBEmployeeId;

        using (var seedScope = PersonnelServiceFactory.CreateScope(WellKnownTenants.SystemTenantId, dbName))
        {
            var db   = seedScope.ServiceProvider.GetRequiredService<PersonnelDbContext>();
            var repo = seedScope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

            var bEmp = Employee.Create(TenantB, "B001", "Carlos", "Ruiz", "carlos@b.com", HireDate);
            repo.Add(bEmp);
            await db.CommitAsync();
            tenantBEmployeeId = bEmp.Id;
        }

        // TenantA tries to look up TenantB's employee by ID
        using var scopeA = PersonnelServiceFactory.CreateScope(TenantA, dbName);
        var repoA = scopeA.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var result = await repoA.GetByIdAsync(tenantBEmployeeId);

        Assert.Null(result);
    }

    // ──────────────────────────────────────────────────────────────────
    // Scenario 3: Hierarchy queries do not cross tenant boundaries
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllSubordinateIds_DoesNotReturn_OtherTenantEmployees()
    {
        // Arrange: TenantA has CEO → VP. TenantB has its own CEO with same hierarchy depth.
        var dbName = Guid.NewGuid().ToString();
        Guid ceoAId, ceoBId;

        using (var seedScope = PersonnelServiceFactory.CreateScope(WellKnownTenants.SystemTenantId, dbName))
        {
            var db   = seedScope.ServiceProvider.GetRequiredService<PersonnelDbContext>();
            var repo = seedScope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

            var ceoA = Employee.Create(TenantA, "CEOA", "CEO", "A", "ceoa@a.com", HireDate);
            repo.Add(ceoA);
            await db.CommitAsync();
            ceoAId = ceoA.Id;

            var vpA = Employee.Create(TenantA, "VPA", "VP", "A", "vpa@a.com", HireDate, managerId: ceoA.Id);
            repo.Add(vpA);
            await db.CommitAsync();

            var ceoB = Employee.Create(TenantB, "CEOB", "CEO", "B", "ceob@b.com", HireDate);
            repo.Add(ceoB);
            await db.CommitAsync();
            ceoBId = ceoB.Id;

            var vpB = Employee.Create(TenantB, "VPB", "VP", "B", "vpb@b.com", HireDate, managerId: ceoB.Id);
            repo.Add(vpB);
            await db.CommitAsync();
        }

        // Act: TenantA repo asks for subordinates of TenantA CEO
        using var scopeA = PersonnelServiceFactory.CreateScope(TenantA, dbName);
        var repoA = scopeA.ServiceProvider.GetRequiredService<IEmployeeRepository>();
        var subordinates = await repoA.GetAllSubordinateIdsAsync(ceoAId);

        // Assert: only TenantA employees returned (CEO_A + VP_A = 2)
        Assert.Equal(2, subordinates.Count);
        Assert.DoesNotContain(ceoBId, subordinates);
    }

    [Fact]
    public async Task IsSubordinateOf_ReturnsFalse_ForCrossTenantPair()
    {
        // Arrange: TenantA has a manager, TenantB has an employee
        var dbName = Guid.NewGuid().ToString();
        Guid managerAId, employeeBId;

        using (var seedScope = PersonnelServiceFactory.CreateScope(WellKnownTenants.SystemTenantId, dbName))
        {
            var db   = seedScope.ServiceProvider.GetRequiredService<PersonnelDbContext>();
            var repo = seedScope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

            var mgrA = Employee.Create(TenantA, "MGRA", "Mgr", "A", "mgra@a.com", HireDate);
            repo.Add(mgrA);
            await db.CommitAsync();
            managerAId = mgrA.Id;

            var empB = Employee.Create(TenantB, "EMPB", "Emp", "B", "empb@b.com", HireDate);
            repo.Add(empB);
            await db.CommitAsync();
            employeeBId = empB.Id;
        }

        // TenantA's repository checks if TenantB's employee is under TenantA's manager
        using var scopeA = PersonnelServiceFactory.CreateScope(TenantA, dbName);
        var repoA = scopeA.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var result = await repoA.IsSubordinateOfAsync(employeeBId, managerAId);

        // Cross-tenant: no closure row exists, must return false
        Assert.False(result);
    }

    // ──────────────────────────────────────────────────────────────────
    // Scenario 4: HierarchyScopeResolver respects tenant boundary
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task HierarchyScopeResolver_ResolveAllSubordinates_ScopedToCurrentTenant()
    {
        var dbName = Guid.NewGuid().ToString();
        Guid ceoAId;

        using (var seedScope = PersonnelServiceFactory.CreateScope(WellKnownTenants.SystemTenantId, dbName))
        {
            var db   = seedScope.ServiceProvider.GetRequiredService<PersonnelDbContext>();
            var repo = seedScope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

            var ceoA = Employee.Create(TenantA, "CEOA", "CEO", "A", "ceoa@a.com", HireDate);
            repo.Add(ceoA);
            await db.CommitAsync();
            ceoAId = ceoA.Id;

            // 3 reports for TenantA
            for (var i = 0; i < 3; i++)
            {
                repo.Add(Employee.Create(TenantA, $"EmpA{i}", "Emp", $"A{i}", $"empa{i}@a.com", HireDate, managerId: ceoA.Id));
            }
            await db.CommitAsync();

            // 5 employees for TenantB with their own CEO
            var ceoB = Employee.Create(TenantB, "CEOB", "CEO", "B", "ceob@b.com", HireDate);
            repo.Add(ceoB);
            await db.CommitAsync();
            for (var i = 0; i < 4; i++)
            {
                repo.Add(Employee.Create(TenantB, $"EmpB{i}", "Emp", $"B{i}", $"empb{i}@b.com", HireDate, managerId: ceoB.Id));
            }
            await db.CommitAsync();
        }

        // Act: TenantA's scope resolver
        using var scopeA = PersonnelServiceFactory.CreateScope(TenantA, dbName);
        var resolver = scopeA.ServiceProvider.GetRequiredService<IHierarchyScopeResolver>();

        var resolved = await resolver.ResolveAllSubordinatesAsync(ceoAId);

        // Must be 4: CEO_A + 3 reports. TenantB's 5 employees must NOT appear.
        Assert.Equal(4, resolved.Count);
    }
}
