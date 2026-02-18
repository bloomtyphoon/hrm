using HRM.BuildingBlocks.Infrastructure.Extensions;
using HRM.Modules.Organization.Api.Contracts;
using HRM.Modules.Organization.Application.Commands.ActivatePosition;
using HRM.Modules.Organization.Application.Commands.ClosePosition;
using HRM.Modules.Organization.Application.Commands.CreatePosition;
using HRM.Modules.Organization.Application.Commands.DeactivatePosition;
using HRM.Modules.Organization.Application.Commands.MovePositionToDepartment;
using HRM.Modules.Organization.Application.Commands.UpdatePosition;
using HRM.Modules.Organization.Application.DTOs;
using HRM.Modules.Organization.Application.Queries.GetPositionById;
using HRM.Modules.Organization.Application.Queries.GetPositionsByCompany;
using HRM.Modules.Organization.Application.Queries.GetPositionsByDepartment;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HRM.Modules.Organization.Api.Endpoints;

public static class PositionEndpoints
{
    public static IEndpointRouteBuilder MapPositionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organization/positions")
            .WithTags("Positions")
            .RequireAuthorization();

        group.MapPost("/", CreatePosition)
            .WithName("CreatePosition")
            .WithSummary("Create a new position")
            .WithDescription("Create a new position within a company. Optionally linked to a department.")
            .Produces<PositionResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/", GetPositionsByCompany)
            .WithName("GetPositionsByCompany")
            .WithSummary("Get positions by company")
            .WithDescription("Retrieve all positions for a given company.")
            .Produces<IReadOnlyList<PositionResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/{id:guid}", GetPositionById)
            .WithName("GetPositionById")
            .WithSummary("Get position by ID")
            .WithDescription("Retrieve a position by its ID.")
            .Produces<PositionResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/by-department/{departmentId:guid}", GetPositionsByDepartment)
            .WithName("GetPositionsByDepartment")
            .WithSummary("Get positions by department")
            .WithDescription("Retrieve all positions for a given department.")
            .Produces<IReadOnlyList<PositionResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapPut("/{id:guid}", UpdatePosition)
            .WithName("UpdatePosition")
            .WithSummary("Update a position")
            .WithDescription("Update position title, level, management flag, description, and headcount.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/move", MovePositionToDepartment)
            .WithName("MovePositionToDepartment")
            .WithSummary("Move position to department")
            .WithDescription("Move a position to a different department or remove department association.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/activate", ActivatePosition)
            .WithName("ActivatePosition")
            .WithSummary("Activate a position")
            .WithDescription("Set position status to Active.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/deactivate", DeactivatePosition)
            .WithName("DeactivatePosition")
            .WithSummary("Deactivate a position")
            .WithDescription("Set position status to Inactive.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/close", ClosePosition)
            .WithName("ClosePosition")
            .WithSummary("Close a position")
            .WithDescription("Set position status to Closed. Position will no longer be available for assignment.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> CreatePosition(
        CreatePositionRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new CreatePositionCommand(
            CompanyId: request.CompanyId,
            Code: request.Code,
            Title: request.Title,
            PositionLevel: request.PositionLevel,
            IsManagement: request.IsManagement,
            DepartmentId: request.DepartmentId,
            Description: request.Description,
            MaxHeadcount: request.MaxHeadcount
        );

        var result = await sender.Send(command, cancellationToken);

        return await result.ToHttpResultAsync(async positionId =>
        {
            var query = new GetPositionByIdQuery(positionId);
            var position = await sender.Send(query, cancellationToken);

            if (position is null)
            {
                return Results.Problem(
                    detail: "Position was created but could not be retrieved.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }

            return Results.Created($"/api/organization/positions/{positionId}", MapToResponse(position));
        });
    }

    private static async Task<IResult> GetPositionsByCompany(
        Guid companyId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetPositionsByCompanyQuery(companyId);
        var positions = await sender.Send(query, cancellationToken);

        var response = positions.Select(MapToResponse).ToList();
        return Results.Ok(response);
    }

    private static async Task<IResult> GetPositionById(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetPositionByIdQuery(id);
        var position = await sender.Send(query, cancellationToken);

        if (position is null)
        {
            return Results.NotFound(new
            {
                Code = "Position.NotFound",
                Message = $"Position with ID '{id}' was not found."
            });
        }

        return Results.Ok(MapToResponse(position));
    }

    private static async Task<IResult> GetPositionsByDepartment(
        Guid departmentId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetPositionsByDepartmentQuery(departmentId);
        var positions = await sender.Send(query, cancellationToken);

        var response = positions.Select(MapToResponse).ToList();
        return Results.Ok(response);
    }

    private static async Task<IResult> UpdatePosition(
        Guid id,
        UpdatePositionRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new UpdatePositionCommand(
            PositionId: id,
            Title: request.Title,
            PositionLevel: request.PositionLevel,
            IsManagement: request.IsManagement,
            Description: request.Description,
            MaxHeadcount: request.MaxHeadcount
        );

        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> MovePositionToDepartment(
        Guid id,
        MovePositionToDepartmentRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new MovePositionToDepartmentCommand(
            PositionId: id,
            DepartmentId: request.DepartmentId
        );

        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> ActivatePosition(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new ActivatePositionCommand(id);
        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> DeactivatePosition(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new DeactivatePositionCommand(id);
        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> ClosePosition(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new ClosePositionCommand(id);
        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static PositionResponse MapToResponse(PositionDto dto) =>
        new(
            Id: dto.Id,
            Code: dto.Code,
            Title: dto.Title,
            Description: dto.Description,
            CompanyId: dto.CompanyId,
            DepartmentId: dto.DepartmentId,
            PositionLevel: dto.PositionLevel,
            IsManagement: dto.IsManagement,
            MaxHeadcount: dto.MaxHeadcount,
            Status: dto.Status,
            CreatedAtUtc: dto.CreatedAtUtc,
            ModifiedAtUtc: dto.ModifiedAtUtc
        );
}
