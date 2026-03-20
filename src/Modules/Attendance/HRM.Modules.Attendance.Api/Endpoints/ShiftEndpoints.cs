using HRM.BuildingBlocks.Application.Pagination;
using HRM.BuildingBlocks.Infrastructure.Extensions;
using HRM.Modules.Attendance.Api.Contracts;
using HRM.Modules.Attendance.Application.Commands.AssignShift;
using HRM.Modules.Attendance.Application.Commands.CreateShift;
using HRM.Modules.Attendance.Application.Commands.DeleteShift;
using HRM.Modules.Attendance.Application.Commands.UpdateShift;
using HRM.Modules.Attendance.Application.Queries.GetShiftAssignments;
using HRM.Modules.Attendance.Application.Queries.GetShifts;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HRM.Modules.Attendance.Api.Endpoints;

public static class ShiftEndpoints
{
    public static IEndpointRouteBuilder MapShiftEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/attendance/shifts")
            .WithTags("Shifts")
            .RequireAuthorization();

        group.MapGet("/", GetShifts)
            .WithName("GetShifts")
            .WithSummary("Get all shifts within scope")
            .Produces<PagedResult<ShiftDto>>(StatusCodes.Status200OK);

        group.MapPost("/", CreateShift)
            .WithName("CreateShift")
            .WithSummary("Create a new shift")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:guid}", UpdateShift)
            .WithName("UpdateShift")
            .WithSummary("Update an existing shift")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}", DeleteShift)
            .WithName("DeleteShift")
            .WithSummary("Delete a shift")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Shift Assignments
        var assignments = app.MapGroup("/api/attendance/shift-assignments")
            .WithTags("Shift Assignments")
            .RequireAuthorization();

        assignments.MapGet("/", GetShiftAssignments)
            .WithName("GetShiftAssignments")
            .WithSummary("Get shift assignments within scope")
            .Produces<PagedResult<ShiftAssignmentDto>>(StatusCodes.Status200OK);

        assignments.MapPost("/", AssignShift)
            .WithName("AssignShift")
            .WithSummary("Assign an employee to a shift")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }

    private static async Task<IResult> GetShifts(
        ISender sender,
        CancellationToken cancellationToken,
        bool? isActive = null,
        int pageNumber = 1,
        int pageSize = 20)
    {
        var query = new GetShiftsQuery
        {
            IsActive = isActive,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        var result = await sender.Send(query, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateShift(
        CreateShiftRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new CreateShiftCommand(
            request.Name,
            request.StartTime,
            request.EndTime,
            request.CompanyId,
            request.Description);

        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult(id => Results.Created($"/api/attendance/shifts/{id}", new { Id = id }));
    }

    private static async Task<IResult> UpdateShift(
        Guid id,
        UpdateShiftRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new UpdateShiftCommand(id, request.Name, request.StartTime, request.EndTime, request.Description);
        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> DeleteShift(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new DeleteShiftCommand(id);
        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetShiftAssignments(
        ISender sender,
        CancellationToken cancellationToken,
        Guid? employeeId = null,
        Guid? shiftId = null,
        int pageNumber = 1,
        int pageSize = 20)
    {
        var query = new GetShiftAssignmentsQuery
        {
            EmployeeId = employeeId,
            ShiftId = shiftId,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        var result = await sender.Send(query, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> AssignShift(
        AssignShiftRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new AssignShiftCommand(
            request.ShiftId,
            request.EmployeeId,
            request.EffectiveFrom,
            request.EffectiveTo,
            request.CompanyId);

        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult(id => Results.Created($"/api/attendance/shift-assignments/{id}", new { Id = id }));
    }
}
