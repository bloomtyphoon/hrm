using HRM.Modules.Personnel.Application.Abstractions;
using HRM.Modules.Personnel.Domain.Entities;
using HRM.Modules.Personnel.Infrastructure.Persistence;
using HRM.Modules.Personnel.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HRM.Modules.Personnel.Tests.Tests;

/// <summary>
/// Tests for manager change event propagation through the closure table.
///
/// Each test verifies the Celko 2-step prune+graft algorithm executed by
/// ManagerChangedDomainEventHandler in response to Employee.AssignManager()
/// and Employee.RemoveManager() domain events.
///
/// Hierarchy:
///   AssignManager() → raises ManagerChangedDomainEvent → prune old ancestors + graft new subtree
///   RemoveManager() → raises ManagerChangedDomainEvent (NewManagerId = null) → prune only
/// </summary>
public class ManagerChangedPropagationTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly DateOnly HireDate = new(2020, 1, 1);

    // ──────────────────────────────────────────────────────────────────
    // Scenario 1: Assigning a manager for the first time
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AssignManager_ClosureTableUpdatedCorrectly()
    {
        // Arrange: CEO and an unmanaged employee, both created without manager
        using var scope = PersonnelServiceFactory.CreateScope(TenantId);
        var db   = scope.ServiceProvider.GetRequiredService<PersonnelDbContext>();
        var repo = scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var ceo = Employee.Create(TenantId, "CEO001", "Chief", "Exec", "ceo@test.com", HireDate);
        var emp = Employee.Create(TenantId, "EMP001", "Emp", "One", "emp@test.com", HireDate);
        repo.Add(ceo);
        repo.Add(emp);
        await db.CommitAsync();

        // Initially: 2 self-ref rows only
        var rowsBefore = await db.EmployeeHierarchyClosures.Where(c => c.TenantId == TenantId).CountAsync();
        Assert.Equal(2, rowsBefore);

        // Act: assign CEO as manager of emp (raises ManagerChangedDomainEvent)
        var empLoaded = (await repo.GetByIdAsync(emp.Id))!;
        empLoaded.AssignManager(ceo.Id);
        repo.Update(empLoaded);
        await db.CommitAsync();

        // Assert: emp now has 2 rows: self + CEO as ancestor at depth 1
        var rowsAfter = await db.EmployeeHierarchyClosures.Where(c => c.TenantId == TenantId).CountAsync();
        Assert.Equal(3, rowsAfter); // CEO: 1 self-ref, emp: self-ref + CEO-ancestor = 2

        Assert.True(await repo.IsSubordinateOfAsync(emp.Id, ceo.Id));
        Assert.False(await repo.IsSubordinateOfAsync(ceo.Id, emp.Id));
    }

    [Fact]
    public async Task AssignManager_SubtreeCorrectlyGrafted_WhenEmployeeHasReports()
    {
        // Arrange: Build  A → B → C  then re-parent B under a new root D
        //
        //   Before:  A - B - C       After:  A    D - B - C
        using var scope = PersonnelServiceFactory.CreateScope(TenantId);
        var db   = scope.ServiceProvider.GetRequiredService<PersonnelDbContext>();
        var repo = scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var a = Employee.Create(TenantId, "AAA", "A", "Root", "a@test.com", HireDate);
        repo.Add(a);
        await db.CommitAsync();

        var b = Employee.Create(TenantId, "BBB", "B", "Mid", "b@test.com", HireDate, managerId: a.Id);
        repo.Add(b);
        await db.CommitAsync();

        var c = Employee.Create(TenantId, "CCC", "C", "Leaf", "c@test.com", HireDate, managerId: b.Id);
        repo.Add(c);
        await db.CommitAsync();

        var d = Employee.Create(TenantId, "DDD", "D", "NewRoot", "d@test.com", HireDate);
        repo.Add(d);
        await db.CommitAsync();

        // Act: re-parent B (and its subtree) under D instead of A
        var bLoaded = (await repo.GetByIdAsync(b.Id))!;
        bLoaded.AssignManager(d.Id);
        repo.Update(bLoaded);
        await db.CommitAsync();

        // Assert: C should now be a subordinate of D (through B), but NOT of A
        Assert.True(await repo.IsSubordinateOfAsync(c.Id, d.Id),  "C should be under D after graft");
        Assert.True(await repo.IsSubordinateOfAsync(b.Id, d.Id),  "B should be under D after graft");
        Assert.False(await repo.IsSubordinateOfAsync(c.Id, a.Id), "C should NOT be under A after prune");
        Assert.False(await repo.IsSubordinateOfAsync(b.Id, a.Id), "B should NOT be under A after prune");

        // D's subtree: D + B + C = 3
        var dSubtree = await repo.GetAllSubordinateIdsAsync(d.Id);
        Assert.Equal(3, dSubtree.Count);

        // A's subtree: A only = 1 (B moved away)
        var aSubtree = await repo.GetAllSubordinateIdsAsync(a.Id);
        Assert.Equal(1, aSubtree.Count);
        Assert.Contains(a.Id, aSubtree);
    }

    // ──────────────────────────────────────────────────────────────────
    // Scenario 2: Removing a manager (employee becomes root)
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RemoveManager_PrunesAncestorRows_LeavingSelfRefOnly()
    {
        // Arrange: CEO → VP → Employee
        using var scope = PersonnelServiceFactory.CreateScope(TenantId);
        var db   = scope.ServiceProvider.GetRequiredService<PersonnelDbContext>();
        var repo = scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var ceo = Employee.Create(TenantId, "CEO001", "Chief", "Exec", "ceo@test.com", HireDate);
        repo.Add(ceo);
        await db.CommitAsync();

        var vp = Employee.Create(TenantId, "VP001", "VP", "One", "vp@test.com", HireDate, managerId: ceo.Id);
        repo.Add(vp);
        await db.CommitAsync();

        var emp = Employee.Create(TenantId, "EMP001", "Emp", "One", "emp@test.com", HireDate, managerId: vp.Id);
        repo.Add(emp);
        await db.CommitAsync();

        // Verify initial: emp has 3 rows (self, VP-ancestor, CEO-ancestor)
        var empRowsBefore = await db.EmployeeHierarchyClosures
            .Where(c => c.TenantId == TenantId && c.DescendantId == emp.Id)
            .CountAsync();
        Assert.Equal(3, empRowsBefore);

        // Act: remove VP's manager (VP becomes root)
        var vpLoaded = (await repo.GetByIdAsync(vp.Id))!;
        vpLoaded.RemoveManager();
        repo.Update(vpLoaded);
        await db.CommitAsync();

        // Assert: VP and emp should no longer be under CEO
        Assert.False(await repo.IsSubordinateOfAsync(vp.Id, ceo.Id),  "VP should not be under CEO after removal");
        Assert.False(await repo.IsSubordinateOfAsync(emp.Id, ceo.Id), "emp should not be under CEO after VP removal");

        // VP's own ancestor chain should be just itself
        var vpChain = await repo.GetManagementChainAsync(vp.Id);
        Assert.Single(vpChain);
        Assert.Equal(vp.Id, vpChain[0]);

        // emp's chain: emp → VP (CEO gone)
        var empChain = await repo.GetManagementChainAsync(emp.Id);
        Assert.Equal(2, empChain.Count);
        Assert.Equal(emp.Id, empChain[0]);
        Assert.Equal(vp.Id,  empChain[1]);
    }

    // ──────────────────────────────────────────────────────────────────
    // Scenario 3: Chain of manager changes — closure stays consistent
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task MultipleManagerChanges_ClosureRemainsConsistent()
    {
        // Scenario: Employee moves A→B→C→A (circular-ish reassignments, not circular in tree)
        // We use 3 separate sibling managers and keep moving the leaf around.
        using var scope = PersonnelServiceFactory.CreateScope(TenantId);
        var db   = scope.ServiceProvider.GetRequiredService<PersonnelDbContext>();
        var repo = scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var root = Employee.Create(TenantId, "ROOT", "Root", "Emp", "root@test.com", HireDate);
        repo.Add(root);
        await db.CommitAsync();

        var m1 = Employee.Create(TenantId, "MGR1", "Mgr", "One",   "m1@test.com", HireDate, managerId: root.Id);
        var m2 = Employee.Create(TenantId, "MGR2", "Mgr", "Two",   "m2@test.com", HireDate, managerId: root.Id);
        var m3 = Employee.Create(TenantId, "MGR3", "Mgr", "Three", "m3@test.com", HireDate, managerId: root.Id);
        repo.Add(m1);
        repo.Add(m2);
        repo.Add(m3);
        await db.CommitAsync();

        var leaf = Employee.Create(TenantId, "LEAF", "Leaf", "Emp", "leaf@test.com", HireDate, managerId: m1.Id);
        repo.Add(leaf);
        await db.CommitAsync();

        // Move leaf: m1 → m2
        var l = (await repo.GetByIdAsync(leaf.Id))!;
        l.AssignManager(m2.Id);
        repo.Update(l);
        await db.CommitAsync();

        Assert.True(await repo.IsSubordinateOfAsync(leaf.Id, m2.Id));
        Assert.False(await repo.IsSubordinateOfAsync(leaf.Id, m1.Id));

        // Move leaf: m2 → m3
        l = (await repo.GetByIdAsync(leaf.Id))!;
        l.AssignManager(m3.Id);
        repo.Update(l);
        await db.CommitAsync();

        Assert.True(await repo.IsSubordinateOfAsync(leaf.Id, m3.Id));
        Assert.False(await repo.IsSubordinateOfAsync(leaf.Id, m2.Id));

        // Move leaf back: m3 → m1
        l = (await repo.GetByIdAsync(leaf.Id))!;
        l.AssignManager(m1.Id);
        repo.Update(l);
        await db.CommitAsync();

        Assert.True(await repo.IsSubordinateOfAsync(leaf.Id, m1.Id));
        Assert.False(await repo.IsSubordinateOfAsync(leaf.Id, m3.Id));

        // root should always see all 5 employees (self + 3 managers + 1 leaf)
        var rootSubtree = await repo.GetAllSubordinateIdsAsync(root.Id);
        Assert.Equal(5, rootSubtree.Count);

        // m1 sees self + leaf = 2; m2 and m3 see only themselves
        Assert.Equal(2, (await repo.GetAllSubordinateIdsAsync(m1.Id)).Count);
        Assert.Equal(1, (await repo.GetAllSubordinateIdsAsync(m2.Id)).Count);
        Assert.Equal(1, (await repo.GetAllSubordinateIdsAsync(m3.Id)).Count);
    }

    // ──────────────────────────────────────────────────────────────────
    // Scenario 4: IsSubordinateOfAsync self-reference guard
    // ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task IsSubordinateOf_ReturnsFalse_ForSelf()
    {
        using var scope = PersonnelServiceFactory.CreateScope(TenantId);
        var db   = scope.ServiceProvider.GetRequiredService<PersonnelDbContext>();
        var repo = scope.ServiceProvider.GetRequiredService<IEmployeeRepository>();

        var emp = Employee.Create(TenantId, "EMP001", "Emp", "One", "emp@test.com", HireDate);
        repo.Add(emp);
        await db.CommitAsync();

        // An employee is NOT considered their own subordinate
        Assert.False(await repo.IsSubordinateOfAsync(emp.Id, emp.Id));
    }
}
