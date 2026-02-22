using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Personnel;
using HRM.BuildingBlocks.Domain.Abstractions.Permissions;
using HRM.BuildingBlocks.Domain.Abstractions.UnitOfWork;
using HRM.BuildingBlocks.Infrastructure.Security;
using HRM.Modules.Personnel.Application;
using HRM.Modules.Personnel.Application.Abstractions;
using HRM.Modules.Personnel.Application.Abstractions.Data;
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
        var connectionString = configuration.GetConnectionString("HrmDb");
        services.AddDbContext<PersonnelDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString);
        });

        // Unit of Work
        services.AddScoped<IModuleUnitOfWork>(sp => sp.GetRequiredService<PersonnelDbContext>());

        // Query Context
        services.AddScoped<IPersonnelQueryContext>(sp => sp.GetRequiredService<PersonnelDbContext>());

        // Scope Services
        services.AddScoped<IDataScopeService, DataScopeService>();
        services.AddScoped<IHierarchyScopeResolver, HierarchyScopeResolver>();
        services.AddScoped<DataScopePolicyService>();

        // MediatR handlers in Infrastructure (domain event handlers)
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
        });

        // Repositories
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IEmployeeAssignmentQuery, EmployeeAssignmentQuery>();

        // Cross-module query (consumed by Organization and other modules)
        services.AddScoped<IPersonnelQuery, PersonnelQueryService>();

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
