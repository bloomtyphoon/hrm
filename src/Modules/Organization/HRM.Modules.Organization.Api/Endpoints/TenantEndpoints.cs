using HRM.BuildingBlocks.Infrastructure.Extensions;
using HRM.Modules.Organization.Api.Contracts;
using HRM.Modules.Organization.Application.Commands.ActivateTenant;
using HRM.Modules.Organization.Application.Commands.CreateTenant;
using HRM.Modules.Organization.Application.Commands.DeactivateTenant;
using HRM.Modules.Organization.Application.Commands.SuspendTenant;
using HRM.Modules.Organization.Application.Commands.UpdateTenant;
using HRM.Modules.Organization.Application.DTOs;
using HRM.Modules.Organization.Application.Queries.GetTenantById;
using HRM.Modules.Organization.Application.Queries.GetTenants;
using HRM.Modules.Organization.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HRM.Modules.Organization.Api.Endpoints;

public static class TenantEndpoints
{
    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organization/tenants")
            .WithTags("Tenants")
            .RequireAuthorization();

        group.MapPost("/", CreateTenant)
            .WithName("CreateTenant")
            .WithSummary("Create a new tenant")
            .Produces<TenantResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/", GetTenants)
            .WithName("GetTenants")
            .WithSummary("Get all tenants")
            .Produces<IReadOnlyList<TenantResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/{id:guid}", GetTenantById)
            .WithName("GetTenantById")
            .WithSummary("Get tenant by ID")
            .Produces<TenantResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", UpdateTenant)
            .WithName("UpdateTenant")
            .WithSummary("Update a tenant")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}/activate", ActivateTenant)
            .WithName("ActivateTenant")
            .WithSummary("Activate a tenant")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}/suspend", SuspendTenant)
            .WithName("SuspendTenant")
            .WithSummary("Suspend a tenant")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}/deactivate", DeactivateTenant)
            .WithName("DeactivateTenant")
            .WithSummary("Deactivate a tenant")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // Public endpoint — no auth required; used by login page to identify tenant from subdomain
        app.MapGet("/api/organization/tenants/by-subdomain/{subdomain}", ResolveSubdomain)
            .WithName("ResolveTenantBySubdomain")
            .WithSummary("Resolve tenant by subdomain (public)")
            .WithTags("Tenants")
            .Produces<TenantSubdomainResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .AllowAnonymous();

        return app;
    }

    private static async Task<IResult> CreateTenant(
        CreateTenantRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new CreateTenantCommand(
            Code: request.Code,
            Name: request.Name,
            Subdomain: request.Subdomain);

        var result = await sender.Send(command, cancellationToken);

        return await result.ToHttpResultAsync(async tenantId =>
        {
            var query = new GetTenantByIdQuery(tenantId);
            var tenant = await sender.Send(query, cancellationToken);

            if (tenant is null)
            {
                return Results.Problem(
                    detail: "Tenant was created but could not be retrieved.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            return Results.Created($"/api/organization/tenants/{tenantId}", MapToResponse(tenant));
        });
    }

    private static async Task<IResult> GetTenants(
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetTenantsQuery();
        var tenants = await sender.Send(query, cancellationToken);
        return Results.Ok(tenants.Select(MapToResponse).ToList());
    }

    private static async Task<IResult> GetTenantById(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetTenantByIdQuery(id);
        var tenant = await sender.Send(query, cancellationToken);

        if (tenant is null)
        {
            return Results.NotFound(new
            {
                Code = "Tenant.NotFound",
                Message = $"Tenant with ID '{id}' was not found."
            });
        }

        return Results.Ok(MapToResponse(tenant));
    }

    private static async Task<IResult> UpdateTenant(
        Guid id,
        UpdateTenantRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new UpdateTenantCommand(
            TenantId: id,
            Name: request.Name,
            Subdomain: request.Subdomain);

        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> ActivateTenant(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new ActivateTenantCommand(id);
        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> SuspendTenant(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new SuspendTenantCommand(id);
        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> DeactivateTenant(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new DeactivateTenantCommand(id);
        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> ResolveSubdomain(
        string subdomain,
        ITenantRepository tenantRepository,
        CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetBySubdomainAsync(
            subdomain.ToLowerInvariant(), cancellationToken);

        if (tenant is null || tenant.Status != HRM.Modules.Organization.Domain.Entities.TenantStatus.Active)
        {
            return Results.NotFound(new
            {
                Code = "Tenant.SubdomainNotFound",
                Message = $"No active tenant found for subdomain '{subdomain}'."
            });
        }

        return Results.Ok(new TenantSubdomainResponse(
            Id: tenant.Id,
            Code: tenant.Code,
            Name: tenant.Name,
            Status: tenant.Status.ToString()));
    }

    private static TenantResponse MapToResponse(TenantDto dto) =>
        new(
            Id: dto.Id,
            Code: dto.Code,
            Name: dto.Name,
            Status: dto.Status,
            IsSystemTenant: dto.IsSystemTenant,
            Subdomain: dto.Subdomain,
            CreatedAtUtc: dto.CreatedAtUtc,
            ModifiedAtUtc: dto.ModifiedAtUtc);
}
