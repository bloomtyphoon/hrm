using HRM.BuildingBlocks.Application.Pagination;
using HRM.BuildingBlocks.Infrastructure.Extensions;
using HRM.Modules.Identity.Api.Contracts;
using HRM.Modules.Identity.Application.Commands.CreateRole;
using HRM.Modules.Identity.Application.Commands.DeleteRole;
using HRM.Modules.Identity.Application.Commands.UpdateRole;
using HRM.Modules.Identity.Application.Queries.GetRoleById;
using HRM.Modules.Identity.Application.Queries.GetRoles;
using HRM.Modules.Identity.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HRM.Modules.Identity.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for Role management.
/// </summary>
public static class RoleEndpoints
{
    public static IEndpointRouteBuilder MapRoleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/identity/roles")
            .WithTags("Roles")
            .RequireAuthorization();

        group.MapGet("/", GetRoles)
            .WithName("GetRoles")
            .WithSummary("Get paginated list of roles")
            .Produces<PagedResult<RoleSummaryDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/{id:guid}", GetRoleById)
            .WithName("GetRoleById")
            .WithSummary("Get role by ID with permissions")
            .Produces<RoleResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateRole)
            .WithName("CreateRole")
            .WithSummary("Create a new role")
            .Produces<RoleResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}", UpdateRole)
            .WithName("UpdateRole")
            .WithSummary("Update an existing role")
            .Produces<RoleResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}", DeleteRole)
            .WithName("DeleteRole")
            .WithSummary("Delete a role")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetRoles(
        ISender sender,
        string? searchTerm = null,
        bool? isSystemRole = null,
        Guid? companyId = null,
        bool allCompanies = false,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = new GetRolesQuery
        {
            SearchTerm = searchTerm,
            IsSystemRole = isSystemRole,
            CompanyId = companyId,
            AllCompanies = allCompanies,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await sender.Send(query, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetRoleById(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetRoleByIdQuery(id);
        var result = await sender.Send(query, cancellationToken);

        return result.ToHttpResult(dto => Results.Ok(new RoleResponse(
            Id: dto.Id,
            Name: dto.Name,
            Description: dto.Description,
            IsSystemRole: dto.IsSystemRole,
            CompanyId: dto.CompanyId,
            PermissionCount: dto.PermissionCount,
            Permissions: dto.Permissions.Select(p => new RolePermissionResponse(
                Module: p.Module,
                Entity: p.Entity,
                Action: p.Action,
                Scope: p.Scope,
                PermissionKey: p.PermissionKey
            )).ToList(),
            CreatedAtUtc: dto.CreatedAtUtc,
            ModifiedAtUtc: dto.ModifiedAtUtc
        )));
    }

    private static async Task<IResult> CreateRole(
        CreateRoleRequest request,
        ISender sender,
        IRoleRepository roleRepository,
        CancellationToken cancellationToken)
    {
        var command = new CreateRoleCommand(
            Name: request.Name,
            Description: request.Description,
            IsSystemRole: request.IsSystemRole,
            CompanyId: request.CompanyId,
            Permissions: request.Permissions.Select(p => new PermissionDto(
                Module: p.Module,
                Entity: p.Entity,
                Action: p.Action,
                Scope: p.Scope
            )).ToList()
        );

        var result = await sender.Send(command, cancellationToken);

        return await result.ToHttpResultAsync(async roleId =>
        {
            var role = await roleRepository.GetByIdAsync(roleId, cancellationToken);

            if (role is null)
            {
                return Results.Problem(
                    detail: "Role was created but could not be retrieved.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }

            var response = new RoleResponse(
                Id: role.Id,
                Name: role.Name,
                Description: role.Description,
                IsSystemRole: role.IsSystemRole,
                CompanyId: role.CompanyId,
                PermissionCount: role.PermissionCount,
                Permissions: role.Permissions.Select(p => new RolePermissionResponse(
                    Module: p.Module,
                    Entity: p.Entity,
                    Action: p.Action,
                    Scope: p.Scope,
                    PermissionKey: p.PermissionKey
                )).ToList(),
                CreatedAtUtc: role.CreatedAtUtc,
                ModifiedAtUtc: role.ModifiedAtUtc
            );

            return Results.Created($"/api/identity/roles/{roleId}", response);
        });
    }

    private static async Task<IResult> UpdateRole(
        Guid id,
        UpdateRoleRequest request,
        ISender sender,
        IRoleRepository roleRepository,
        CancellationToken cancellationToken)
    {
        var command = new UpdateRoleCommand(
            RoleId: id,
            Name: request.Name,
            Description: request.Description,
            Permissions: request.Permissions.Select(p => new PermissionDto(
                Module: p.Module,
                Entity: p.Entity,
                Action: p.Action,
                Scope: p.Scope
            )).ToList()
        );

        var result = await sender.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            var role = await roleRepository.GetByIdAsync(id, cancellationToken);

            if (role is null)
            {
                return Results.Problem(
                    detail: "Role was updated but could not be retrieved.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }

            var response = new RoleResponse(
                Id: role.Id,
                Name: role.Name,
                Description: role.Description,
                IsSystemRole: role.IsSystemRole,
                CompanyId: role.CompanyId,
                PermissionCount: role.PermissionCount,
                Permissions: role.Permissions.Select(p => new RolePermissionResponse(
                    Module: p.Module,
                    Entity: p.Entity,
                    Action: p.Action,
                    Scope: p.Scope,
                    PermissionKey: p.PermissionKey
                )).ToList(),
                CreatedAtUtc: role.CreatedAtUtc,
                ModifiedAtUtc: role.ModifiedAtUtc
            );

            return Results.Ok(response);
        }

        return result.ToHttpResult();
    }

    private static async Task<IResult> DeleteRole(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new DeleteRoleCommand(id);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return Results.NoContent();
        }

        return result.ToHttpResult();
    }
}
