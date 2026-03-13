using HRM.BuildingBlocks.Application.DependencyInjection;
using HRM.BuildingBlocks.Infrastructure.DependencyInjection;
using HRM.Modules.Attendance.Api.DependencyInjection;
using HRM.Modules.Attendance.Application.DependencyInjection;
using HRM.Modules.Attendance.Infrastructure;
using HRM.Modules.Identity.Api.DependencyInjection;
using HRM.Modules.Identity.Application.DependencyInjection;
using HRM.Modules.Identity.Infrastructure.DependencyInjection;
using HRM.Modules.Organization.Api.DependencyInjection;
using HRM.Modules.Organization.Application.DependencyInjection;
using HRM.Modules.Organization.Infrastructure;
using HRM.Modules.Personnel.Api.DependencyInjection;
using HRM.Modules.Personnel.Application.DependencyInjection;
using HRM.Modules.Personnel.Infrastructure;

namespace HRM.Api.DependencyInjection;

/// <summary>
/// Extension methods for registering all HRM modules
/// Implements Modular Monolith pattern with module composition
///
/// Architecture:
/// - BuildingBlocks: Shared infrastructure (MediatR, Authentication, EventBus, etc.)
/// - Identity Module: Authentication and authorization (Accounts)
/// - Personnel Module: Employee management
/// - Attendance Module: Time tracking
///
/// Module Registration Order (CRITICAL - DO NOT CHANGE):
/// 1. BuildingBlocks Application (MediatR + pipeline behaviors)
/// 2. BuildingBlocks Infrastructure (technical services)
/// 3. Module-specific Application (handlers + validators)
/// 4. Module-specific Infrastructure (DbContext + UnitOfWork)
///
/// Usage (Program.cs):
/// <code>
/// // Register all modules
/// builder.Services.AddModules(builder.Configuration);
///
/// // Map all module endpoints
/// app.MapModuleEndpoints();
/// </code>
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Register all HRM modules with dependency injection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Application configuration</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddModules(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ====================================================================
        // REGISTRATION ORDER IS CRITICAL - DO NOT REORDER
        // ====================================================================

        // 1. BuildingBlocks Application Layer
        // Register MediatR with pipeline behaviors (BEFORE any handlers)
        // - LoggingBehavior (outermost - logs all requests)
        // - ValidationBehavior (fails fast before transaction)
        // - UnitOfWorkBehavior (wraps handler with transaction)
        services.AddBuildingBlocksApplication();

        // 2. BuildingBlocks Infrastructure Layer
        // Register technical services (AFTER MediatR, BEFORE modules)
        // - CurrentUserService (for ICurrentUserService)
        // - RolesClaimsTransformation (JWT role normalization)
        // - AuditInterceptor (Scoped - depends on ICurrentUserService)
        services.AddBuildingBlocksInfrastructure(configuration);

        // 3. Identity Module Application Layer
        // Register module-specific handlers and validators
        // - Command handlers (RegisterAccountCommandHandler, etc.)
        // - Query handlers (GetAccountByIdQueryHandler, etc.)
        // - Domain event handlers (AccountCreatedDomainEventHandler, etc.)
        // - FluentValidation validators (RegisterAccountCommandValidator, etc.)
        services.AddIdentityApplication();

        // 4. Identity Module Infrastructure Layer
        // Register module-specific technical implementations
        // - IdentityDbContext (SQL Server, schema: Identity)
        // - IModuleUnitOfWork → IdentityDbContext (for UnitOfWorkBehavior)
        // - Repositories (IAccountRepository → AccountRepository)
        // - Authentication services (IPasswordHasher, ITokenService)
        // - Background services (IdentityOutboxProcessor)
        services.AddIdentityInfrastructure(configuration);

        // 5. Organization Module Application Layer
        // Register module-specific handlers and validators
        // - Command handlers (CreateCompanyCommandHandler, etc.)
        services.AddOrganizationApplication();

        // 6. Organization Module Infrastructure Layer
        // Register module-specific technical implementations
        // - OrganizationDbContext (SQL Server, schema: Organization)
        // - Repositories (ICompanyRepository → CompanyRepository)
        services.AddOrganizationModule(configuration);

        // 7. Personnel Module Application Layer
        services.AddPersonnelApplication();

        // 8. Personnel Module Infrastructure Layer
        // NOTE: Registers IEmployeeScopeDimensionProvider and IPersonnelQuery — must come BEFORE Attendance
        services.AddPersonnelModule(configuration);

        // 9. Attendance Module Application Layer
        services.AddAttendanceApplication();

        // 10. Attendance Module Infrastructure Layer
        services.AddAttendanceModule(configuration);

        return services;
    }

    /// <summary>
    /// Map all module endpoints to the application
    /// </summary>
    /// <param name="app">Endpoint route builder (WebApplication)</param>
    /// <returns>Endpoint route builder for chaining</returns>
    public static IEndpointRouteBuilder MapModuleEndpoints(this IEndpointRouteBuilder app)
    {
        // Map Identity module endpoints
        // - POST /api/identity/accounts/register
        // - POST /api/identity/accounts/{id}/activate
        app.MapIdentityEndpoints();

        // Map Organization module endpoints
        // - POST /api/organization/companies
        // - GET /api/organization/companies
        // - GET /api/organization/companies/{id}
        app.MapOrganizationEndpoints();

        // Map Personnel module endpoints
        app.MapPersonnelEndpoints();

        // Map Attendance module endpoints
        app.MapAttendanceEndpoints();

        return app;
    }
}
