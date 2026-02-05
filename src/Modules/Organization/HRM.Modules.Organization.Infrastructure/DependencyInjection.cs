using HRM.BuildingBlocks.Domain.Abstractions.UnitOfWork;
using HRM.Modules.Organization.Domain.Repositories;
using HRM.Modules.Organization.Infrastructure.Persistence;
using HRM.Modules.Organization.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using HRM.BuildingBlocks.Application.Abstractions.Organization;
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
        var connectionString = configuration.GetConnectionString("HrmDb");
        services.AddDbContext<OrganizationDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString);
        });

        // Unit of Work
        services.AddScoped<IModuleUnitOfWork>(sp => sp.GetRequiredService<OrganizationDbContext>());

        // Repositories
        services.AddScoped<ICompanyRepository, CompanyRepository>();

        // Cross-module query interface (to be implemented)
        // services.AddScoped<IOrganizationQuery, OrganizationQuery>();
        // Cross-module query interface (to be implemented)
        // services.AddScoped<IOrganizationQuery, OrganizationQuery>();

        // Repositories (to be implemented)
        // services.AddScoped<ICompanyRepository, CompanyRepository>();
        // services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        // services.AddScoped<IPositionRepository, PositionRepository>();

        return services;
    }
}
