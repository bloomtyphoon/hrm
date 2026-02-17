using HRM.BuildingBlocks.Infrastructure.Extensions;
using HRM.Modules.Organization.Api.Contracts;
using HRM.Modules.Organization.Application.Commands.CreateDepartment;
using HRM.Modules.Organization.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HRM.Modules.Organization.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for Department operations.
/// </summary>
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

        return app;
    }

    private static async Task<IResult> CreateDepartment(
        CreateDepartmentRequest request,
        ISender sender,
        IDepartmentRepository departmentRepository,
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
            var department = await departmentRepository.GetByIdAsync(departmentId, cancellationToken);

            if (department is null)
            {
                return Results.Problem(
                    detail: "Department was created but could not be retrieved.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }

            var response = new DepartmentResponse(
                Id: department.Id,
                Code: department.Code,
                Name: department.Name,
                CompanyId: department.CompanyId,
                ParentDepartmentId: department.ParentDepartmentId,
                ManagerId: department.ManagerId,
                Level: department.Level,
                Status: department.Status.ToString(),
                CreatedAtUtc: department.CreatedAtUtc,
                ModifiedAtUtc: department.ModifiedAtUtc
            );

            return Results.Created($"/api/organization/departments/{departmentId}", response);
        });
    }

    private static async Task<IResult> GetDepartmentsByCompany(
        Guid companyId,
        IDepartmentRepository departmentRepository,
        CancellationToken cancellationToken)
    {
        var departments = await departmentRepository.GetByCompanyIdAsync(companyId, cancellationToken);

        var response = departments.Select(d => new DepartmentResponse(
            Id: d.Id,
            Code: d.Code,
            Name: d.Name,
            CompanyId: d.CompanyId,
            ParentDepartmentId: d.ParentDepartmentId,
            ManagerId: d.ManagerId,
            Level: d.Level,
            Status: d.Status.ToString(),
            CreatedAtUtc: d.CreatedAtUtc,
            ModifiedAtUtc: d.ModifiedAtUtc
        )).ToList();

        return Results.Ok(response);
    }

    private static async Task<IResult> GetDepartmentById(
        Guid id,
        IDepartmentRepository departmentRepository,
        CancellationToken cancellationToken)
    {
        var department = await departmentRepository.GetByIdAsync(id, cancellationToken);

        if (department is null)
        {
            return Results.NotFound(new
            {
                Code = "Department.NotFound",
                Message = $"Department with ID '{id}' was not found."
            });
        }

        var response = new DepartmentResponse(
            Id: department.Id,
            Code: department.Code,
            Name: department.Name,
            CompanyId: department.CompanyId,
            ParentDepartmentId: department.ParentDepartmentId,
            ManagerId: department.ManagerId,
            Level: department.Level,
            Status: department.Status.ToString(),
            CreatedAtUtc: department.CreatedAtUtc,
            ModifiedAtUtc: department.ModifiedAtUtc
        );

        return Results.Ok(response);
    }
}
