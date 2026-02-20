using HRM.Modules.Identity.Api.Endpoints;
using Microsoft.AspNetCore.Routing;

namespace HRM.Modules.Identity.Api.DependencyInjection;

/// <summary>
/// Extension methods for registering Identity API endpoints.
/// Maps Minimal API routes for account operations.
///
/// Endpoints Registered:
/// - POST /api/identity/accounts/register
/// - POST /api/identity/accounts/{id}/activate
/// - POST /api/identity/auth/login
/// - POST /api/identity/auth/logout
/// - POST /api/identity/auth/refresh
/// - GET /api/identity/auth/sessions
/// - DELETE /api/identity/auth/sessions/{id}
/// - POST /api/identity/auth/sessions/revoke-all-except-current
/// </summary>
public static class IdentityApiExtensions
{
    /// <summary>
    /// Map Identity module endpoints.
    /// </summary>
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder app)
    {
        // Map account management endpoints
        app.MapAccountEndpoints();

        // Map authentication endpoints (login, logout, refresh, sessions)
        app.MapAuthenticationEndpoints();

        // Map role management endpoints (CRUD)
        app.MapRoleEndpoints();

        // Map permission catalog endpoints
        app.MapPermissionEndpoints();

        // Map profile management endpoints
        app.MapSystemProfileEndpoints();
        app.MapEmployeeProfileEndpoints();

        return app;
    }
}
