using HRM.Modules.Organization.Api.Endpoints;
using Microsoft.AspNetCore.Routing;

namespace HRM.Modules.Organization.Api.DependencyInjection;

/// <summary>
/// Extension methods for registering Organization API endpoints.
/// Maps Minimal API routes for company, department, and position operations.
///
/// Company Endpoints:
/// - POST   /api/organization/companies
/// - GET    /api/organization/companies
/// - GET    /api/organization/companies/{id}
/// - PUT    /api/organization/companies/{id}
/// - PUT    /api/organization/companies/{id}/activate
/// - PUT    /api/organization/companies/{id}/deactivate
///
/// Department Endpoints:
/// - POST   /api/organization/departments
/// - GET    /api/organization/departments
/// - GET    /api/organization/departments/{id}
/// - PUT    /api/organization/departments/{id}
/// - PUT    /api/organization/departments/{id}/move
/// - PUT    /api/organization/departments/{id}/manager
/// - DELETE /api/organization/departments/{id}/manager
/// - PUT    /api/organization/departments/{id}/activate
/// - PUT    /api/organization/departments/{id}/deactivate
///
/// Position Endpoints:
/// - POST   /api/organization/positions
/// - GET    /api/organization/positions
/// - GET    /api/organization/positions/{id}
/// - GET    /api/organization/positions/by-department/{departmentId}
/// - PUT    /api/organization/positions/{id}
/// - PUT    /api/organization/positions/{id}/move
/// - PUT    /api/organization/positions/{id}/activate
/// - PUT    /api/organization/positions/{id}/deactivate
/// - PUT    /api/organization/positions/{id}/close
/// </summary>
public static class OrganizationApiExtensions
{
    /// <summary>
    /// Map Organization module endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder app)
    {
        // Map company management endpoints
        app.MapCompanyEndpoints();

        // Map department management endpoints
        app.MapDepartmentEndpoints();

        // Map position management endpoints
        app.MapPositionEndpoints();

        return app;
    }
}
