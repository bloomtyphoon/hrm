using HRM.BuildingBlocks.Infrastructure.Extensions;
using HRM.Modules.Organization.Api.Contracts;
using HRM.Modules.Organization.Application.Commands.CreatePosition;
using HRM.Modules.Organization.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HRM.Modules.Organization.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for Position operations.
/// </summary>
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

        return app;
    }

    private static async Task<IResult> CreatePosition(
        CreatePositionRequest request,
        ISender sender,
        IPositionRepository positionRepository,
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
            var position = await positionRepository.GetByIdAsync(positionId, cancellationToken);

            if (position is null)
            {
                return Results.Problem(
                    detail: "Position was created but could not be retrieved.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }

            var response = new PositionResponse(
                Id: position.Id,
                Code: position.Code,
                Title: position.Title,
                Description: position.Description,
                CompanyId: position.CompanyId,
                DepartmentId: position.DepartmentId,
                PositionLevel: position.PositionLevel,
                IsManagement: position.IsManagement,
                MaxHeadcount: position.MaxHeadcount,
                Status: position.Status.ToString(),
                CreatedAtUtc: position.CreatedAtUtc,
                ModifiedAtUtc: position.ModifiedAtUtc
            );

            return Results.Created($"/api/organization/positions/{positionId}", response);
        });
    }

    private static async Task<IResult> GetPositionsByCompany(
        Guid companyId,
        IPositionRepository positionRepository,
        CancellationToken cancellationToken)
    {
        var positions = await positionRepository.GetByCompanyIdAsync(companyId, cancellationToken);

        var response = positions.Select(p => new PositionResponse(
            Id: p.Id,
            Code: p.Code,
            Title: p.Title,
            Description: p.Description,
            CompanyId: p.CompanyId,
            DepartmentId: p.DepartmentId,
            PositionLevel: p.PositionLevel,
            IsManagement: p.IsManagement,
            MaxHeadcount: p.MaxHeadcount,
            Status: p.Status.ToString(),
            CreatedAtUtc: p.CreatedAtUtc,
            ModifiedAtUtc: p.ModifiedAtUtc
        )).ToList();

        return Results.Ok(response);
    }

    private static async Task<IResult> GetPositionById(
        Guid id,
        IPositionRepository positionRepository,
        CancellationToken cancellationToken)
    {
        var position = await positionRepository.GetByIdAsync(id, cancellationToken);

        if (position is null)
        {
            return Results.NotFound(new
            {
                Code = "Position.NotFound",
                Message = $"Position with ID '{id}' was not found."
            });
        }

        var response = new PositionResponse(
            Id: position.Id,
            Code: position.Code,
            Title: position.Title,
            Description: position.Description,
            CompanyId: position.CompanyId,
            DepartmentId: position.DepartmentId,
            PositionLevel: position.PositionLevel,
            IsManagement: position.IsManagement,
            MaxHeadcount: position.MaxHeadcount,
            Status: position.Status.ToString(),
            CreatedAtUtc: position.CreatedAtUtc,
            ModifiedAtUtc: position.ModifiedAtUtc
        );

        return Results.Ok(response);
    }
}
