using HRM.BuildingBlocks.Infrastructure.Extensions;
using HRM.Modules.Identity.Api.Contracts;
using HRM.Modules.Identity.Application.Commands.CreateEmployeeProfile;
using HRM.Modules.Identity.Application.Commands.UpdateEmployeeProfile;
using HRM.Modules.Identity.Application.Queries.GetEmployeeProfile;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HRM.Modules.Identity.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for EmployeeProfile operations.
/// </summary>
public static class EmployeeProfileEndpoints
{
    public static IEndpointRouteBuilder MapEmployeeProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/identity/accounts/{accountId:guid}/employee-profile")
            .WithTags("Employee Profiles")
            .RequireAuthorization();

        group.MapGet("/", GetEmployeeProfile)
            .WithName("GetEmployeeProfile")
            .WithSummary("Get employee profile for an account")
            .WithDescription("Retrieve the employee profile associated with an employee account.")
            .Produces<EmployeeProfileDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateEmployeeProfile)
            .WithName("CreateEmployeeProfile")
            .WithSummary("Create employee profile")
            .WithDescription("Create an employee profile linking an employee account to an employee entity.")
            .Produces<object>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/", UpdateEmployeeProfile)
            .WithName("UpdateEmployeeProfile")
            .WithSummary("Update employee profile")
            .WithDescription("Update employee profile assignments and scope settings.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetEmployeeProfile(
        Guid accountId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetEmployeeProfileQuery(accountId);
        var result = await sender.Send(query, cancellationToken);

        return result.ToHttpResult(profile => Results.Ok(profile));
    }

    private static async Task<IResult> CreateEmployeeProfile(
        Guid accountId,
        CreateEmployeeProfileRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new CreateEmployeeProfileCommand(
            AccountId: accountId,
            EmployeeId: request.EmployeeId,
            DefaultScopeLevel: request.DefaultScopeLevel,
            PrimaryCompanyId: request.PrimaryCompanyId,
            PrimaryDepartmentId: request.PrimaryDepartmentId,
            PrimaryPositionId: request.PrimaryPositionId);

        var result = await sender.Send(command, cancellationToken);

        return result.ToHttpResult(profileId =>
            Results.Created($"/api/identity/accounts/{accountId}/employee-profile", new { Id = profileId }));
    }

    private static async Task<IResult> UpdateEmployeeProfile(
        Guid accountId,
        UpdateEmployeeProfileRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new UpdateEmployeeProfileCommand(
            AccountId: accountId,
            PrimaryCompanyId: request.PrimaryCompanyId,
            PrimaryDepartmentId: request.PrimaryDepartmentId,
            PrimaryPositionId: request.PrimaryPositionId,
            DefaultScopeLevel: request.DefaultScopeLevel,
            CanAccessAllAssignedCompanies: request.CanAccessAllAssignedCompanies);

        var result = await sender.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return Results.NoContent();
        }

        return result.ToHttpResult();
    }
}
