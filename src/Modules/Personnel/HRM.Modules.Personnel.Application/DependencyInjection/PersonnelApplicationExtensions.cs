using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace HRM.Modules.Personnel.Application.DependencyInjection;

/// <summary>
/// DI registration for Personnel Application layer.
/// </summary>
public static class PersonnelApplicationExtensions
{
    public static IServiceCollection AddPersonnelApplication(this IServiceCollection services)
    {
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(
                typeof(PersonnelApplicationExtensions).Assembly
            );
        });

        services.AddValidatorsFromAssembly(
            typeof(PersonnelApplicationExtensions).Assembly
        );

        return services;
    }
}
