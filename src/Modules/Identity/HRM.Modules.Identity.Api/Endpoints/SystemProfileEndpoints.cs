using HRM.BuildingBlocks.Infrastructure.Extensions;
using HRM.Modules.Identity.Api.Contracts;
using HRM.Modules.Identity.Application.Commands.CreateSystemProfile;
using HRM.Modules.Identity.Application.Commands.GrantSuperAdmin;
using HRM.Modules.Identity.Application.Commands.RevokeSuperAdmin;
using HRM.Modules.Identity.Application.Commands.UpdateSystemProfile;
using HRM.Modules.Identity.Application.Queries.GetSystemProfile;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HRM.Modules.Identity.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for SystemProfile operations.
/// </summary>
public static class SystemProfileEndpoints
{
    public static IEndpointRouteBuilder MapSystemProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/identity/accounts/{accountId:guid}/system-profile")
            .WithTags("System Profiles")
            .RequireAuthorization();

        group.MapGet("/", GetSystemProfile)
            .WithName("GetSystemProfile")
            .WithSummary("Get system profile for an account")
            .WithDescription("Retrieve the system profile associated with a system account.")
            .Produces<SystemProfileDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateSystemProfile)
            .WithName("CreateSystemProfile")
            .WithSummary("Create system profile")
            .WithDescription("Create a system profile for a system account. Only one profile per account.")
            .Produces<object>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/", UpdateSystemProfile)
            .WithName("UpdateSystemProfile")
            .WithSummary("Update system profile")
            .WithDescription("Update system profile information (department, job title, notes).")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/grant-super-admin", GrantSuperAdmin)
            .WithName("GrantSuperAdmin")
            .WithSummary("Grant super admin privileges")
            .WithDescription("Grant super admin privileges to a system account. Bypasses all permission checks.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/revoke-super-admin", RevokeSuperAdmin)
            .WithName("RevokeSuperAdmin")
            .WithSummary("Revoke super admin privileges")
            .WithDescription("Revoke super admin privileges from a system account.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> GetSystemProfile(
        Guid accountId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetSystemProfileQuery(accountId);
        var result = await sender.Send(query, cancellationToken);

        return result.ToHttpResult(profile => Results.Ok(profile));
    }

    private static async Task<IResult> CreateSystemProfile(
        Guid accountId,
        CreateSystemProfileRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new CreateSystemProfileCommand(
            AccountId: accountId,
            IsSuperAdmin: request.IsSuperAdmin,
            Department: request.Department,
            JobTitle: request.JobTitle);

        var result = await sender.Send(command, cancellationToken);

        return result.ToHttpResult(profileId =>
            Results.Created($"/api/identity/accounts/{accountId}/system-profile", new { Id = profileId }));
    }

    private static async Task<IResult> UpdateSystemProfile(
        Guid accountId,
        UpdateSystemProfileRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new UpdateSystemProfileCommand(
            AccountId: accountId,
            Department: request.Department,
            JobTitle: request.JobTitle,
            Notes: request.Notes);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return Results.NoContent();
        }

        return result.ToHttpResult();
    }

    private static async Task<IResult> GrantSuperAdmin(
        Guid accountId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new GrantSuperAdminCommand(AccountId: accountId);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return Results.NoContent();
        }

        return result.ToHttpResult();
    }

    private static async Task<IResult> RevokeSuperAdmin(
        Guid accountId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new RevokeSuperAdminCommand(AccountId: accountId);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return Results.NoContent();
        }

        return result.ToHttpResult();
    }
}
