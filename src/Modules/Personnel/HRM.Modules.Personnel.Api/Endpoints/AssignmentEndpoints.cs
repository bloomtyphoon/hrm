using HRM.BuildingBlocks.Infrastructure.Extensions;
using HRM.Modules.Personnel.Api.Contracts;
using HRM.Modules.Personnel.Application.Abstractions;
using HRM.Modules.Personnel.Application.Commands.AddAssignment;
using HRM.Modules.Personnel.Application.Queries.GetEmployeeAssignments;
using HRM.Modules.Personnel.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HRM.Modules.Personnel.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for Employee Assignment operations.
/// </summary>
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
            .WithDescription("Assign an employee to a company/department/position.")
            .Produces<AssignmentResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/", GetAssignments)
            .WithName("GetAssignments")
            .WithSummary("Get employee assignments")
            .WithDescription("Retrieve all assignments for an employee.")
            .Produces<List<AssignmentDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

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
            // Reload employee with assignments to get the new assignment
            var employee = await employeeRepository.GetWithAssignmentsAsync(employeeId, cancellationToken);
            var assignment = employee?.Assignments.FirstOrDefault(a => a.Id == assignmentId);

            if (assignment is null)
            {
                return Results.Problem(
                    detail: "Assignment was created but could not be retrieved.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }

            var response = MapToResponse(assignment);
            return Results.Created(
                $"/api/personnel/employees/{employeeId}/assignments/{assignmentId}",
                response);
        });
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
