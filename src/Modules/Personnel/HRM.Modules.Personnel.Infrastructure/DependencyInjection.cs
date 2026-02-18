using HRM.BuildingBlocks.Application.Abstractions.Authorization;
using HRM.BuildingBlocks.Domain.Abstractions.UnitOfWork;
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

        // Repositories
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IEmployeeAssignmentQuery, EmployeeAssignmentQuery>();

        return services;
    }
}
