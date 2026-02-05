using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.Modules.Personnel.Infrastructure.Services;
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
    public static IServiceCollection AddPersonnelModule(this IServiceCollection services)
    {
        // Scope Services
        services.AddScoped<IDataScopeService, DataScopeService>();
        services.AddScoped<IHierarchyScopeResolver, HierarchyScopeResolver>();
        services.AddScoped<DataScopePolicyService>();

        // Repositories (to be implemented)
        // services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        // services.AddScoped<IEmployeeAssignmentQuery, EmployeeAssignmentQuery>();

        return services;
    }
}
