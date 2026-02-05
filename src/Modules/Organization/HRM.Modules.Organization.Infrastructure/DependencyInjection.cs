using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.Modules.Organization.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HRM.Modules.Organization.Infrastructure;

/// <summary>
/// Dependency injection registration for Organization module.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Add Organization module services to the DI container.
    /// </summary>
    public static IServiceCollection AddOrganizationModule(this IServiceCollection services)
    {
        // Services
        services.AddScoped<IDataScopeService, DataScopeService>();
        services.AddScoped<IHierarchyScopeResolver, HierarchyScopeResolver>();
        services.AddScoped<DataScopePolicyService>();

        // Repositories (to be implemented)
        // services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        // services.AddScoped<IEmployeeAssignmentQuery, EmployeeAssignmentQuery>();

        return services;
    }
}
