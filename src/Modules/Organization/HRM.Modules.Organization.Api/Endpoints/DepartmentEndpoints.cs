using HRM.BuildingBlocks.Infrastructure.Extensions;
using HRM.Modules.Organization.Api.Contracts;
using HRM.Modules.Organization.Application.Commands.ActivateDepartment;
using HRM.Modules.Organization.Application.Commands.AssignDepartmentManager;
using HRM.Modules.Organization.Application.Commands.CreateDepartment;
using HRM.Modules.Organization.Application.Commands.DeactivateDepartment;
using HRM.Modules.Organization.Application.Commands.MoveDepartment;
using HRM.Modules.Organization.Application.Commands.RemoveDepartmentManager;
using HRM.Modules.Organization.Application.Commands.UpdateDepartment;
using HRM.Modules.Organization.Application.DTOs;
using HRM.Modules.Organization.Application.Queries.GetDepartmentById;
using HRM.Modules.Organization.Application.Queries.GetDepartmentsByCompany;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HRM.Modules.Organization.Api.Endpoints;

public static class DepartmentEndpoints
{
    public static IEndpointRouteBuilder MapDepartmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organization/departments")
            .WithTags("Departments")
            .RequireAuthorization();

        group.MapPost("/", CreateDepartment)
            .WithName("CreateDepartment")
            .WithSummary("Create a new department")
            .WithDescription("Create a new department within a company. Supports hierarchical structure via optional parent department.")
            .Produces<DepartmentResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/", GetDepartmentsByCompany)
            .WithName("GetDepartmentsByCompany")
            .WithSummary("Get departments by company")
            .WithDescription("Retrieve all departments for a given company.")
            .Produces<IReadOnlyList<DepartmentResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/{id:guid}", GetDepartmentById)
            .WithName("GetDepartmentById")
            .WithSummary("Get department by ID")
            .WithDescription("Retrieve a department by its ID.")
            .Produces<DepartmentResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", UpdateDepartment)
            .WithName("UpdateDepartment")
            .WithSummary("Update a department")
            .WithDescription("Update department name and manager.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/move", MoveDepartment)
            .WithName("MoveDepartment")
            .WithSummary("Move department to new parent")
            .WithDescription("Move a department to a new parent department or make it a root department.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/manager", AssignManager)
            .WithName("AssignDepartmentManager")
            .WithSummary("Assign manager to department")
            .WithDescription("Assign a manager to the department.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}/manager", RemoveManager)
            .WithName("RemoveDepartmentManager")
            .WithSummary("Remove manager from department")
            .WithDescription("Remove the manager from the department.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/activate", ActivateDepartment)
            .WithName("ActivateDepartment")
            .WithSummary("Activate a department")
            .WithDescription("Set department status to Active.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/deactivate", DeactivateDepartment)
            .WithName("DeactivateDepartment")
            .WithSummary("Deactivate a department")
            .WithDescription("Set department status to Inactive.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> CreateDepartment(
        CreateDepartmentRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new CreateDepartmentCommand(
            CompanyId: request.CompanyId,
            Code: request.Code,
            Name: request.Name,
            ParentDepartmentId: request.ParentDepartmentId,
            ManagerId: request.ManagerId
        );

        var result = await sender.Send(command, cancellationToken);

        return await result.ToHttpResultAsync(async departmentId =>
        {
            var query = new GetDepartmentByIdQuery(departmentId);
            var department = await sender.Send(query, cancellationToken);

            if (department is null)
            {
                return Results.Problem(
                    detail: "Department was created but could not be retrieved.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }

            return Results.Created($"/api/organization/departments/{departmentId}", MapToResponse(department));
        });
    }

    private static async Task<IResult> GetDepartmentsByCompany(
        Guid companyId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetDepartmentsByCompanyQuery(companyId);
        var departments = await sender.Send(query, cancellationToken);

        var response = departments.Select(MapToResponse).ToList();
        return Results.Ok(response);
    }

    private static async Task<IResult> GetDepartmentById(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetDepartmentByIdQuery(id);
        var department = await sender.Send(query, cancellationToken);

        if (department is null)
        {
            return Results.NotFound(new
            {
                Code = "Department.NotFound",
                Message = $"Department with ID '{id}' was not found."
            });
        }

        return Results.Ok(MapToResponse(department));
    }

    private static async Task<IResult> UpdateDepartment(
        Guid id,
        UpdateDepartmentRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new UpdateDepartmentCommand(
            DepartmentId: id,
            Name: request.Name,
            ManagerId: request.ManagerId
        );

        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> MoveDepartment(
        Guid id,
        MoveDepartmentRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new MoveDepartmentCommand(
            DepartmentId: id,
            NewParentDepartmentId: request.NewParentDepartmentId
        );

        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> AssignManager(
        Guid id,
        AssignDepartmentManagerRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new AssignDepartmentManagerCommand(
            DepartmentId: id,
            ManagerId: request.ManagerId
        );

        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> RemoveManager(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new RemoveDepartmentManagerCommand(id);
        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> ActivateDepartment(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new ActivateDepartmentCommand(id);
        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> DeactivateDepartment(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new DeactivateDepartmentCommand(id);
        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static DepartmentResponse MapToResponse(DepartmentDto dto) =>
        new(
            Id: dto.Id,
            Code: dto.Code,
            Name: dto.Name,
            CompanyId: dto.CompanyId,
            ParentDepartmentId: dto.ParentDepartmentId,
            ManagerId: dto.ManagerId,
            Level: dto.Level,
            Status: dto.Status,
            CreatedAtUtc: dto.CreatedAtUtc,
            ModifiedAtUtc: dto.ModifiedAtUtc
        );
}
