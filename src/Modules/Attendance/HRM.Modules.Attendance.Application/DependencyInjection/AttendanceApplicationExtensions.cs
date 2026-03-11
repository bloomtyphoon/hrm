using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace HRM.Modules.Attendance.Application.DependencyInjection;

/// <summary>
/// DI registration for Attendance Application layer.
/// </summary>
public static class AttendanceApplicationExtensions
{
    public static IServiceCollection AddAttendanceApplication(this IServiceCollection services)
    {
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(
                typeof(AttendanceApplicationExtensions).Assembly
            );
        });

        services.AddValidatorsFromAssembly(
            typeof(AttendanceApplicationExtensions).Assembly
        );

        return services;
    }
}
