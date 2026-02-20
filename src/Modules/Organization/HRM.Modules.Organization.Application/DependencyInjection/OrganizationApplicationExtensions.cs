using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace HRM.Modules.Organization.Application.DependencyInjection;

/// <summary>
/// Dependency injection registration for Organization module Application layer.
///
/// Responsibilities:
/// - Register MediatR command/query handlers
/// - Register FluentValidation validators
/// - Module-specific application services
///
/// Usage (API Program.cs):
/// <code>
/// builder.Services
///     .AddBuildingBlocksApplication()
///     .AddBuildingBlocksInfrastructure()
///     .AddOrganizationApplication()
///     .AddOrganizationInfrastructure();
/// </code>
/// </summary>
public static class OrganizationApplicationExtensions
{
    /// <summary>
    /// Add Organization module Application services.
    /// Registers handlers and validators from Organization.Application assembly.
    /// </summary>
    public static IServiceCollection AddOrganizationApplication(
        this IServiceCollection services)
    {
        // Register MediatR handlers from Organization.Application assembly
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(
                typeof(OrganizationApplicationExtensions).Assembly
            );
        });

        // Register FluentValidation validators from Organization.Application assembly
        services.AddValidatorsFromAssembly(
            typeof(OrganizationApplicationExtensions).Assembly
        );

        return services;
    }
}
