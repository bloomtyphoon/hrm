using HRM.Modules.Organization.Api.Endpoints;
using Microsoft.AspNetCore.Routing;

namespace HRM.Modules.Organization.Api.DependencyInjection;

/// <summary>
/// Extension methods for registering Organization API endpoints.
/// Maps Minimal API routes for company, department, and position operations.
///
/// Endpoints Registered:
/// - POST /api/organization/companies
/// - GET /api/organization/companies
/// - GET /api/organization/companies/{id}
/// - POST /api/organization/departments
/// - GET /api/organization/departments
/// - GET /api/organization/departments/{id}
/// - POST /api/organization/positions
/// - GET /api/organization/positions
/// - GET /api/organization/positions/{id}
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
