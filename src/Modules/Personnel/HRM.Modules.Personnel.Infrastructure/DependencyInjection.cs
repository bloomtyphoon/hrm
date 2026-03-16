using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Personnel;
using HRM.BuildingBlocks.Domain.Abstractions.Permissions;
using HRM.BuildingBlocks.Domain.Abstractions.UnitOfWork;
using HRM.BuildingBlocks.Infrastructure.BackgroundServices;
using HRM.BuildingBlocks.Infrastructure.Security;
using HRM.Modules.Personnel.Application;
using HRM.Modules.Personnel.Application.Abstractions;
using HRM.Modules.Personnel.Application.Abstractions.Data;
using HRM.Modules.Personnel.Infrastructure.BackgroundServices;
using HRM.Modules.Personnel.Infrastructure.Configuration;
using HRM.Modules.Personnel.Infrastructure.Persistence;
using HRM.Modules.Personnel.Infrastructure.Persistence.Repositories;
using HRM.Modules.Personnel.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HRM.Modules.Personnel.Infrastructure;

/// <summary>
/// Dependency injection registration for Personnel module.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Add Personnel module services to the DI container.
    /// </summary>
    public static IServiceCollection AddPersonnelModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // DbContext
        var connectionString = configuration.GetConnectionString("HrmDatabase");
        services.AddDbContext<PersonnelDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString);
        });

        // Unit of Work
        services.AddScoped<IModuleUnitOfWork>(sp => sp.GetRequiredService<PersonnelDbContext>());

        // Query Context
        services.AddScoped<IPersonnelQueryContext>(sp => sp.GetRequiredService<PersonnelDbContext>());

        // Scope Services
        // IDataScopeService is registered in BuildingBlocks.Infrastructure (shared for all modules)
        services.AddScoped<IHierarchyScopeResolver, HierarchyScopeResolver>();
        // MediatR handlers in Infrastructure (domain event handlers)
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
        });

        // Repositories
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IEmployeeAssignmentQuery, EmployeeAssignmentQuery>();

        // Cross-module contracts (consumed by BuildingBlocks DataScopeService and Organization module)
        services.AddScoped<IPersonnelQuery, PersonnelQueryService>();
        services.AddScoped<IEmployeeScopeDimensionProvider, PersonnelQueryService>();

        // Outbox Processor (background service for reliable integration event publishing)
        services.AddHostedService<PersonnelOutboxProcessor>();
        services.Configure<OutboxSettings>(configuration.GetSection(OutboxSettings.SectionName));

        // Hierarchy cache TTL settings (configurable per org size)
        services.Configure<HierarchyCacheSettings>(
            configuration.GetSection(HierarchyCacheSettings.SectionName));

        // Permission catalog source (loaded by IPermissionCatalogService at startup)
        services.AddSingleton<IPermissionCatalogSource>(sp =>
        {
            var factory = sp.GetRequiredService<IPermissionCatalogSourceFactory>();
            return factory.FromEmbeddedResource(
                typeof(PersonnelApplicationAssemblyMarker).Assembly,
                "HRM.Modules.Personnel.Application.Resources.PermissionCatalog.xml");
        });

        // Route security map source (loaded by RouteSecurityLoaderService at startup)
        services.Configure<RouteSecurityOptions>(options =>
        {
            options.Sources.Add(new RouteSecurityMapSourceConfig
            {
                Assembly = typeof(DependencyInjection).Assembly,
                ResourceName = "HRM.Modules.Personnel.Infrastructure.Security.RouteSecurityMap.xml"
            });
        });

        return services;
    }
}
