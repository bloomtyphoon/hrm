using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Application.Abstractions.Organization;
using HRM.BuildingBlocks.Domain.Abstractions.Permissions;
using HRM.BuildingBlocks.Domain.Abstractions.UnitOfWork;
using HRM.BuildingBlocks.Infrastructure.Security;
using HRM.Modules.Organization.Application;
using HRM.Modules.Organization.Domain.Repositories;
using HRM.Modules.Organization.Infrastructure.Persistence;
using HRM.Modules.Organization.Infrastructure.Persistence.Repositories;
using HRM.Modules.Organization.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HRM.Modules.Organization.Infrastructure;

/// <summary>
/// Dependency injection registration for Organization module.
///
/// DESIGN: Organization module owns org STRUCTURE only:
/// - Company, Department, Position
///
/// Employee-related services are in Personnel module.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Add Organization module services to the DI container.
    /// </summary>
    public static IServiceCollection AddOrganizationModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // DbContext
        var connectionString = configuration.GetConnectionString("HrmDatabase");
        services.AddDbContext<OrganizationDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString);
        });

        // Unit of Work
        services.AddScoped<IModuleUnitOfWork>(sp => sp.GetRequiredService<OrganizationDbContext>());

        // Repositories
        services.AddScoped<ICompanyRepository, CompanyRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IPositionRepository, PositionRepository>();

        // Data scope service (uses IScopeGrantProvider + IPersonnelQuery)
        services.AddScoped<IDataScopeService, OrganizationDataScopeService>();

        // Cross-module query interface (to be implemented)
        // services.AddScoped<IOrganizationQuery, OrganizationQuery>();

        // Permission catalog source (loaded by IPermissionCatalogService at startup)
        services.AddSingleton<IPermissionCatalogSource>(sp =>
        {
            var factory = sp.GetRequiredService<IPermissionCatalogSourceFactory>();
            return factory.FromEmbeddedResource(
                typeof(OrganizationApplicationAssemblyMarker).Assembly,
                "HRM.Modules.Organization.Application.Resources.PermissionCatalog.xml");
        });

        // Route security map source (loaded by RouteSecurityLoaderService at startup)
        services.Configure<RouteSecurityOptions>(options =>
        {
            options.Sources.Add(new RouteSecurityMapSourceConfig
            {
                Assembly = typeof(DependencyInjection).Assembly,
                ResourceName = "HRM.Modules.Organization.Infrastructure.Security.RouteSecurityMap.xml"
            });
        });

        return services;
    }
}
