using HRM.Modules.Personnel.Api.Endpoints;
using Microsoft.AspNetCore.Routing;

namespace HRM.Modules.Personnel.Api.DependencyInjection;

/// <summary>
/// Extension methods for registering Personnel API endpoints.
/// </summary>
public static class PersonnelApiExtensions
{
    public static IEndpointRouteBuilder MapPersonnelEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapEmployeeEndpoints();
        app.MapAssignmentEndpoints();

        return app;
    }
}
