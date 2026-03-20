using HRM.BuildingBlocks.Domain.Abstractions.Permissions;
using HRM.BuildingBlocks.Domain.Abstractions.UnitOfWork;
using HRM.BuildingBlocks.Infrastructure.BackgroundServices;
using HRM.BuildingBlocks.Infrastructure.Security;
using HRM.Modules.Attendance.Application;
using HRM.Modules.Attendance.Application.Abstractions;
using HRM.Modules.Attendance.Application.Abstractions.Data;
using HRM.Modules.Attendance.Infrastructure.BackgroundServices;
using HRM.Modules.Attendance.Infrastructure.Persistence;
using HRM.Modules.Attendance.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HRM.Modules.Attendance.Infrastructure;

/// <summary>
/// Dependency injection registration for Attendance module.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddAttendanceModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // DbContext
        var connectionString = configuration.GetConnectionString("HrmDatabase");
        services.AddDbContext<AttendanceDbContext>((sp, options) =>
        {
            options.UseSqlServer(connectionString);
        });

        // Unit of Work
        services.AddScoped<IModuleUnitOfWork>(sp => sp.GetRequiredService<AttendanceDbContext>());

        // Query Context
        services.AddScoped<IAttendanceQueryContext>(sp => sp.GetRequiredService<AttendanceDbContext>());

        // MediatR handlers in Infrastructure (domain event handlers)
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly);
        });

        // Repositories
        services.AddScoped<IAttendanceRecordRepository, AttendanceRecordRepository>();
        services.AddScoped<IShiftRepository, ShiftRepository>();
        services.AddScoped<IShiftAssignmentRepository, ShiftAssignmentRepository>();
        services.AddScoped<ILeaveTypeRepository, LeaveTypeRepository>();
        services.AddScoped<ILeaveRequestRepository, LeaveRequestRepository>();
        services.AddScoped<ILeaveApprovalSettingRepository, LeaveApprovalSettingRepository>();
        services.AddScoped<ILeaveApprovalStepRepository, LeaveApprovalStepRepository>();

        // Approval chain resolver (Manager → DepartmentHead → CompanyLevel)
        services.AddScoped<IApprovalChainResolver, Services.ApprovalChainResolver>();

        // Outbox Processor
        services.AddHostedService<AttendanceOutboxProcessor>();
        services.Configure<OutboxSettings>(configuration.GetSection(OutboxSettings.SectionName));

        // Permission catalog source
        services.AddSingleton<IPermissionCatalogSource>(sp =>
        {
            var factory = sp.GetRequiredService<IPermissionCatalogSourceFactory>();
            return factory.FromEmbeddedResource(
                typeof(AttendanceApplicationAssemblyMarker).Assembly,
                "HRM.Modules.Attendance.Application.Resources.PermissionCatalog.xml");
        });

        // Route security map source
        services.Configure<RouteSecurityOptions>(options =>
        {
            options.Sources.Add(new RouteSecurityMapSourceConfig
            {
                Assembly = typeof(DependencyInjection).Assembly,
                ResourceName = "HRM.Modules.Attendance.Infrastructure.Security.RouteSecurityMap.xml"
            });
        });

        return services;
    }
}
