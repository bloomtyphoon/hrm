using HRM.Modules.Personnel.Application.Abstractions;
using HRM.Modules.Personnel.Domain.Entities;
using HRM.Modules.Personnel.Infrastructure.Persistence;
using HRM.Modules.Personnel.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HRM.Modules.Personnel.Tests.Tests;

/// <summary>
/// Tests for RebuildHierarchyAsync and large-dataset closure table correctness.
///
/// WHAT IS TESTED:
///   - Closure table has the correct row count after building a large hierarchy via event handlers
///   - GetAllSubordinateIdsAsync returns correct results on a multi-level tree
///   - GetManagementChainAsync returns full chain for deeply nested employees
///   - Step 1 + 2 of RebuildHierarchyAsync (clear + self-refs) run without error
///   - Queries still work correctly after partial rebuild (steps 1+2)
///
/// WHAT REQUIRES SQL SERVER (not tested here):
///   - Step 3 of RebuildHierarchyAsync — the recursive CTE uses raw SQL with
///     SQL Server schema-qualified table names (Personnel.Employees).
///     Marked with [Fact(Skip = ...)] below.
/// </summary>
public class RebuildHierarchyTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateOnly HireDate = new(2020, 1, 1);

    // ──────────────────────────────────────────────────────────────────
    // Scenario 1: Large hierarchy built through event handlers
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task LargeHierarchy_ClosureTableHasCorrectRowCount()
    {
        // Arrange: Build a 3-level hierarchy
        //   CEO
        //   ├── VP1 (10 direct reports)
        //   └── VP2 (10 direct reports)
        //   Total: 23 employees

        using var scope = PersonnelServiceFactory.CreateScope(TenantId);
        var db   = scope.ServiceProvider.GetRequiredService<PersonnelDbContext>();
        var repo = scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var ceo = Employee.Create(TenantId, "CEO001", "Chief", "Executive", "ceo@test.com", HireDate);
        repo.Add(ceo);
        await db.CommitAsync();

        var vp1 = Employee.Create(TenantId, "VP001", "VP", "One", "vp1@test.com", HireDate, managerId: ceo.Id);
        var vp2 = Employee.Create(TenantId, "VP002", "VP", "Two", "vp2@test.com", HireDate, managerId: ceo.Id);
        repo.Add(vp1);
        repo.Add(vp2);
        await db.CommitAsync();

        for (var i = 0; i < 10; i++)
        {
            repo.Add(Employee.Create(TenantId, $"EMP1{i:D2}", "Emp", $"Vp1Sub{i}", $"emp1{i}@test.com", HireDate, managerId: vp1.Id));
            repo.Add(Employee.Create(TenantId, $"EMP2{i:D2}", "Emp", $"Vp2Sub{i}", $"emp2{i}@test.com", HireDate, managerId: vp2.Id));
        }
        await db.CommitAsync();

        // Assert: closure rows per employee = (depth + 1) per path
        // CEO: 1 self-ref = 1 row
        // VP1: self + CEO = 2 rows
        // VP2: self + CEO = 2 rows
        // Each leaf under VP1/VP2: self + VP + CEO = 3 rows each (10 * 2 = 20 leaves = 60 rows)
        // Total = 1 + 2 + 2 + 60 = 65
        var totalRows = await db.EmployeeHierarchyClosures
            .Where(c => c.TenantId == TenantId)
            .CountAsync();

        Assert.Equal(65, totalRows);
    }

    [Fact]
    public async Task LargeHierarchy_GetAllSubordinatesReturnsFullSubtree()
    {
        // Arrange: CEO → 5 VPs → 4 direct reports each
        using var scope = PersonnelServiceFactory.CreateScope(TenantId);
        var db   = scope.ServiceProvider.GetRequiredService<PersonnelDbContext>();
        var repo = scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var ceo = Employee.Create(TenantId, "CEO001", "Chief", "Executive", "ceo@test.com", HireDate);
        repo.Add(ceo);
        await db.CommitAsync();

        var vpIds = new List<Guid>();
        for (var v = 0; v < 5; v++)
        {
            var vp = Employee.Create(TenantId, $"VP{v:D3}", "VP", $"Exec{v}", $"vp{v}@test.com", HireDate, managerId: ceo.Id);
            repo.Add(vp);
            vpIds.Add(vp.Id);
        }
        await db.CommitAsync();

        for (var v = 0; v < 5; v++)
        {
            for (var r = 0; r < 4; r++)
            {
                repo.Add(Employee.Create(TenantId, $"R{v}{r:D2}", "Rep", $"Sub{v}{r}", $"r{v}{r}@test.com", HireDate, managerId: vpIds[v]));
            }
        }
        await db.CommitAsync();

        // Act: CEO should see all 26 employees (self + 5 VPs + 20 reps)
        var subtree = await repo.GetAllSubordinateIdsAsync(ceo.Id);

        Assert.Equal(26, subtree.Count);
        Assert.Contains(ceo.Id, subtree);

        // Each VP sees self + 4 reports = 5
        var firstVpSubtree = await repo.GetAllSubordinateIdsAsync(vpIds[0]);
        Assert.Equal(5, firstVpSubtree.Count);
        Assert.Contains(vpIds[0], firstVpSubtree);
    }

    [Fact]
    public async Task DeepChain_GetManagementChainReturnsFullChain()
    {
        // Arrange: 5-level deep chain
        using var scope = PersonnelServiceFactory.CreateScope(TenantId);
        var db   = scope.ServiceProvider.GetRequiredService<PersonnelDbContext>();
        var repo = scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var ids = new List<Guid>();
        Guid? parentId = null;

        for (var level = 0; level < 5; level++)
        {
            var emp = Employee.Create(TenantId, $"LVL{level:D3}", "Level", $"Emp{level}", $"lvl{level}@test.com", HireDate, managerId: parentId);
            repo.Add(emp);
            await db.CommitAsync();
            ids.Add(emp.Id);
            parentId = emp.Id;
        }

        // Act: deepest employee (level 4) → management chain should be [lvl4, lvl3, lvl2, lvl1, lvl0]
        var chain = await repo.GetManagementChainAsync(ids[4]);

        Assert.Equal(5, chain.Count);
        Assert.Equal(ids[4], chain[0]); // self first
        Assert.Equal(ids[3], chain[1]);
        Assert.Equal(ids[2], chain[2]);
        Assert.Equal(ids[1], chain[3]);
        Assert.Equal(ids[0], chain[4]); // root last
    }

    // ──────────────────────────────────────────────────────────────────
    // Scenario 2: RebuildHierarchyAsync — steps 1 and 2 (InMemory-safe)
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RebuildHierarchy_Steps1And2_ClearAndSelfRefsCorrect()
    {
        // Arrange: Build a small hierarchy, then corrupt the closure table
        using var scope = PersonnelServiceFactory.CreateScope(TenantId);
        var db   = scope.ServiceProvider.GetRequiredService<PersonnelDbContext>();
        var repo = scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var ceo  = Employee.Create(TenantId, "CEO001", "Chief", "Executive", "ceo@test.com", HireDate);
        var mgr  = Employee.Create(TenantId, "MGR001", "Manager", "One", "mgr@test.com", HireDate, managerId: ceo.Id);
        var emp  = Employee.Create(TenantId, "EMP001", "Employee", "One", "emp@test.com", HireDate, managerId: mgr.Id);
        repo.Add(ceo);
        await db.CommitAsync();
        repo.Add(mgr);
        await db.CommitAsync();
        repo.Add(emp);
        await db.CommitAsync();

        // Verify initial state: 6 rows (1 + 2 + 3)
        var initialCount = await db.EmployeeHierarchyClosures.Where(c => c.TenantId == TenantId).CountAsync();
        Assert.Equal(6, initialCount);

        // Corrupt: manually remove all closure rows to simulate data loss
        var allRows = await db.EmployeeHierarchyClosures.Where(c => c.TenantId == TenantId).ToListAsync();
        db.EmployeeHierarchyClosures.RemoveRange(allRows);
        await db.SaveChangesAsync();

        Assert.Equal(0, await db.EmployeeHierarchyClosures.CountAsync());

        // Act: Attempt rebuild (Step 3 uses raw SQL — will throw on InMemory, catch gracefully)
        try
        {
            await repo.RebuildHierarchyAsync(TenantId);
        }
        catch (InvalidOperationException)
        {
            // Expected: InMemory provider does not support ExecuteSqlRawAsync.
            // Steps 1 and 2 completed before this throw.
        }

        // Assert: After steps 1+2, we have exactly 3 self-reference rows (one per employee)
        var selfRefCount = await db.EmployeeHierarchyClosures
            .Where(c => c.TenantId == TenantId && c.Depth == 0)
            .CountAsync();

        Assert.Equal(3, selfRefCount);

        // Each self-ref: AncestorId == DescendantId
        var selfRefs = await db.EmployeeHierarchyClosures
            .Where(c => c.TenantId == TenantId && c.Depth == 0)
            .ToListAsync();

        Assert.All(selfRefs, r => Assert.Equal(r.AncestorId, r.DescendantId));
    }

    [Fact(Skip = "Requires SQL Server relational provider — ExecuteSqlRawAsync not supported by InMemory")]
    public Task RebuildHierarchy_Step3_SqlCte_RequiresSqlServer()
    {
        // This test is intentionally skipped for in-memory test runs.
        // To validate the full rebuild path, run against a SQL Server instance.
        return Task.CompletedTask;
    }
}
