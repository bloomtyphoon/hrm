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
    public static IServiceCollection AddOrganizationModule(this IServiceCollection services)
    {
        // Cross-module query interface (to be implemented)
        // services.AddScoped<IOrganizationQuery, OrganizationQuery>();

        // Repositories (to be implemented)
        // services.AddScoped<ICompanyRepository, CompanyRepository>();
        // services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        // services.AddScoped<IPositionRepository, PositionRepository>();

        return services;
    }
}
