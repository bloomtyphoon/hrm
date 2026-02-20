using HRM.BuildingBlocks.Infrastructure.Extensions;
using HRM.Modules.Personnel.Api.Contracts;
using HRM.Modules.Personnel.Application.Abstractions;
using HRM.Modules.Personnel.Application.Commands.AddAssignment;
using HRM.Modules.Personnel.Application.Commands.EndAssignment;
using HRM.Modules.Personnel.Application.Commands.SetPrimaryAssignment;
using HRM.Modules.Personnel.Application.Queries.GetEmployeeAssignments;
using HRM.Modules.Personnel.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HRM.Modules.Personnel.Api.Endpoints;

public static class AssignmentEndpoints
{
    public static IEndpointRouteBuilder MapAssignmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/personnel/employees/{employeeId:guid}/assignments")
            .WithTags("Employee Assignments")
            .RequireAuthorization();

        group.MapPost("/", AddAssignment)
            .WithName("AddAssignment")
            .WithSummary("Add assignment to employee")
            .Produces<AssignmentResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/", GetAssignments)
            .WithName("GetAssignments")
            .WithSummary("Get employee assignments")
            .Produces<List<AssignmentDto>>(StatusCodes.Status200OK);

        group.MapPut("/{assignmentId:guid}/end", EndAssignment)
            .WithName("EndAssignment")
            .WithSummary("End an assignment")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{assignmentId:guid}/primary", SetPrimaryAssignment)
            .WithName("SetPrimaryAssignment")
            .WithSummary("Set an assignment as primary")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> AddAssignment(
        Guid employeeId,
        AddAssignmentRequest request,
        ISender sender,
        IEmployeeRepository employeeRepository,
        CancellationToken cancellationToken)
    {
        var command = new AddAssignmentCommand(
            EmployeeId: employeeId,
            CompanyId: request.CompanyId,
            DepartmentId: request.DepartmentId,
            PositionId: request.PositionId,
            StartDate: request.StartDate,
            IsPrimary: request.IsPrimary
        );

        var result = await sender.Send(command, cancellationToken);

        return await result.ToHttpResultAsync(async assignmentId =>
        {
            var employee = await employeeRepository.GetWithAssignmentsAsync(employeeId, cancellationToken);
            var assignment = employee?.Assignments.FirstOrDefault(a => a.Id == assignmentId);

            if (assignment is null)
            {
                return Results.Problem(
                    detail: "Assignment was created but could not be retrieved.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            var response = MapToResponse(assignment);
            return Results.Created(
                $"/api/personnel/employees/{employeeId}/assignments/{assignmentId}",
                response);
        });
    }

    private static async Task<IResult> EndAssignment(
        Guid employeeId,
        Guid assignmentId,
        EndAssignmentRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new EndAssignmentCommand(
            EmployeeId: employeeId,
            AssignmentId: assignmentId,
            EndDate: request.EndDate
        );

        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> SetPrimaryAssignment(
        Guid employeeId,
        Guid assignmentId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new SetPrimaryAssignmentCommand(
            EmployeeId: employeeId,
            AssignmentId: assignmentId
        );

        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetAssignments(
        Guid employeeId,
        ISender sender,
        CancellationToken cancellationToken,
        AssignmentStatus? status = null)
    {
        var query = new GetEmployeeAssignmentsQuery
        {
            EmployeeId = employeeId,
            Status = status
        };

        var assignments = await sender.Send(query, cancellationToken);
        return Results.Ok(assignments);
    }

    private static AssignmentResponse MapToResponse(EmployeeAssignment assignment) =>
        new(
            Id: assignment.Id,
            EmployeeId: assignment.EmployeeId,
            CompanyId: assignment.CompanyId,
            DepartmentId: assignment.DepartmentId,
            PositionId: assignment.PositionId,
            StartDate: assignment.StartDate,
            EndDate: assignment.EndDate,
            IsPrimary: assignment.IsPrimary,
            Status: assignment.Status.ToString()
        );
}
