using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Multitenancy;
using HRM.BuildingBlocks.Domain.Abstractions.UnitOfWork;
using HRM.Modules.Personnel.Application.Abstractions;
using HRM.Modules.Personnel.Infrastructure.DomainEventHandlers;
using HRM.Modules.Personnel.Infrastructure.Persistence;
using HRM.Modules.Personnel.Infrastructure.Persistence.Repositories;
using HRM.Modules.Personnel.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HRM.Modules.Personnel.Tests.Helpers;

/// <summary>
/// Factory for creating a fully wired DI container for Personnel integration tests.
///
/// Uses EF Core InMemory provider — all LINQ-based operations (closure table
/// queries, manager changes, tenant filtering) work correctly.
///
/// Note: RebuildHierarchyAsync step 3 (SQL recursive CTE) requires a relational
/// provider and is not exercised by these tests — see RebuildHierarchyTests for details.
/// </summary>
internal static class PersonnelServiceFactory
{
    /// <summary>
    /// Creates an isolated service scope for a test.
    /// Each call with a unique dbName gets a fresh in-memory database.
    /// When dbName is omitted, a new Guid is used, guaranteeing test isolation.
    /// </summary>
    /// <param name="tenantId">
    /// Optional tenant context to inject. Null = no tenant filter (system/background).
    /// Pass WellKnownTenants.SystemTenantId for system-admin access.
    /// </param>
    /// <param name="dbName">
    /// In-memory database name. Pass the same name to multiple CreateScope calls
    /// to share data across tenant contexts within a single test.
    /// </param>
    public static IServiceScope CreateScope(Guid? tenantId = null, string? dbName = null)
    {
        var services = new ServiceCollection();
        var name = dbName ?? Guid.NewGuid().ToString();

        // DbContext using EF Core InMemory provider
        services.AddDbContext<PersonnelDbContext>((_, options) =>
            options.UseInMemoryDatabase(name));

        // Unit of Work (delegates to PersonnelDbContext.CommitAsync)
        services.AddScoped<IModuleUnitOfWork>(sp =>
            sp.GetRequiredService<PersonnelDbContext>());

        // Repositories
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();

        // Hierarchy scope resolver
        services.AddScoped<IHierarchyScopeResolver, HierarchyScopeResolver>();

        // MediatR — scans the Infrastructure assembly for all domain event handlers:
        //   EmployeeCreatedHierarchyHandler     (inserts self-ref + parent chain)
        //   ManagerChangedDomainEventHandler    (Celko prune+graft)
        //   EmployeeCreatedDomainEventHandler   (creates outbox message)
        //   EmployeeAssignmentsChangedDomainEventHandler (creates outbox message)
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(ManagerChangedDomainEventHandler).Assembly));

        // Tenant context — drives the global EF query filter on ITenantEntity.
        // When null: filter bypassed (system-level access, matching WellKnownTenants.SystemTenantId).
        // When provided: only entities with matching TenantId are visible.
        if (tenantId.HasValue)
        {
            var captured = tenantId.Value;
            services.AddScoped<ITenantContext>(_ => new FakeTenantContext(captured));
        }

        return services.BuildServiceProvider().CreateScope();
    }
}
