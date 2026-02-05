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
///
/// Future:
/// - /api/organization/departments
/// - /api/organization/positions
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

        // Future: Map other endpoint groups
        // app.MapDepartmentEndpoints();
        // app.MapPositionEndpoints();

        return app;
    }
}
