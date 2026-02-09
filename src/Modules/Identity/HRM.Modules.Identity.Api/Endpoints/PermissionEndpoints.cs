using HRM.Modules.Identity.Application.Queries.GetPermissionCatalog;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HRM.Modules.Identity.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for Permission catalog.
/// </summary>
public static class PermissionEndpoints
{
    public static IEndpointRouteBuilder MapPermissionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/identity/permissions")
            .WithTags("Permissions")
            .RequireAuthorization();

        group.MapGet("/catalog", GetPermissionCatalog)
            .WithName("GetPermissionCatalog")
            .WithSummary("Get the full permission catalog")
            .WithDescription("Returns all available permissions organized by module, entity, and action. Used for role management UI.")
            .Produces<PermissionCatalogDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }

    private static async Task<IResult> GetPermissionCatalog(
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetPermissionCatalogQuery();
        var result = await sender.Send(query, cancellationToken);
        return Results.Ok(result);
    }
}
