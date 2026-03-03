using HRM.BuildingBlocks.Application.Pagination;
using HRM.BuildingBlocks.Infrastructure.Extensions;
using HRM.Modules.Attendance.Api.Contracts;
using HRM.Modules.Attendance.Application.Commands.CheckIn;
using HRM.Modules.Attendance.Application.Commands.CheckOut;
using HRM.Modules.Attendance.Application.Commands.RecordManualAttendance;
using HRM.Modules.Attendance.Application.Queries.GetAttendanceById;
using HRM.Modules.Attendance.Application.Queries.GetEmployeeAttendance;
using HRM.Modules.Attendance.Application.Queries.GetMyAttendance;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Attendance.Api.Endpoints;

public static class AttendanceEndpoints
{
    public static IEndpointRouteBuilder MapAttendanceRecordEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/attendance")
            .WithTags("Attendance")
            .RequireAuthorization();

        // Employee self check-in
        group.MapPost("/check-in", CheckIn)
            .WithName("CheckIn")
            .WithSummary("Record employee check-in for today")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // Employee self check-out
        group.MapPost("/check-out", CheckOut)
            .WithName("CheckOut")
            .WithSummary("Record employee check-out for today's active check-in")
            .Produces<Guid>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Employee views own attendance history
        group.MapGet("/me", GetMyAttendance)
            .WithName("GetMyAttendance")
            .WithSummary("Get current employee's attendance history")
            .Produces<PagedResult<AttendanceSummaryDto>>(StatusCodes.Status200OK);

        // Manager / HR views a specific employee's attendance
        group.MapGet("/employees/{employeeId:guid}", GetEmployeeAttendance)
            .WithName("GetEmployeeAttendance")
            .WithSummary("Get attendance history for a specific employee")
            .Produces<PagedResult<AttendanceSummaryDto>>(StatusCodes.Status200OK);

        // Get a single attendance record by ID
        group.MapGet("/records/{id:guid}", GetAttendanceById)
            .WithName("GetAttendanceById")
            .WithSummary("Get attendance record by ID")
            .Produces<AttendanceRecordDetailDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Manual attendance recording (HR / manager)
        group.MapPost("/records", RecordManualAttendance)
            .WithName("RecordManualAttendance")
            .WithSummary("Manually record attendance for an employee (HR / manager only)")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> CheckIn(
        CheckInRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new CheckInCommand(request.CheckInTimeUtc, request.Notes);

        try
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(id => Results.Created($"/api/attendance/records/{id}", new { Id = id }));
        }
        catch (DbUpdateException)
        {
            // Unique index (EmployeeId, Date) violation — concurrent check-in race condition
            return Results.Conflict(new
            {
                Code = "Attendance.AlreadyCheckedIn",
                Message = "An attendance record already exists for today."
            });
        }
    }

    private static async Task<IResult> CheckOut(
        CheckOutRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new CheckOutCommand(request.CheckOutTimeUtc, request.Notes);
        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult(id => Results.Ok(new { Id = id }));
    }

    private static async Task<IResult> GetMyAttendance(
        ISender sender,
        CancellationToken cancellationToken,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        int pageNumber = 1,
        int pageSize = 20)
    {
        var query = new GetMyAttendanceQuery
        {
            FromDate = fromDate,
            ToDate = toDate,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await sender.Send(query, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetEmployeeAttendance(
        Guid employeeId,
        ISender sender,
        CancellationToken cancellationToken,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        int pageNumber = 1,
        int pageSize = 20)
    {
        var query = new GetEmployeeAttendanceQuery
        {
            EmployeeId = employeeId,
            FromDate = fromDate,
            ToDate = toDate,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await sender.Send(query, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetAttendanceById(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetAttendanceByIdQuery { RecordId = id };
        var record = await sender.Send(query, cancellationToken);

        if (record is null)
        {
            return Results.NotFound(new
            {
                Code = "Attendance.NotFound",
                Message = $"Attendance record with ID '{id}' was not found."
            });
        }

        return Results.Ok(record);
    }

    private static async Task<IResult> RecordManualAttendance(
        RecordManualAttendanceRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new RecordManualAttendanceCommand(
            EmployeeId: request.EmployeeId,
            Date: request.Date,
            CheckInTimeUtc: request.CheckInTimeUtc,
            CheckOutTimeUtc: request.CheckOutTimeUtc,
            CompanyId: request.CompanyId,
            Notes: request.Notes);

        try
        {
            var result = await sender.Send(command, cancellationToken);
            return result.ToHttpResult(id => Results.Created($"/api/attendance/records/{id}", new { Id = id }));
        }
        catch (DbUpdateException)
        {
            // Unique index (EmployeeId, Date) violation
            return Results.Conflict(new
            {
                Code = "Attendance.AlreadyCheckedIn",
                Message = "An attendance record already exists for this employee on the specified date."
            });
        }
    }
}
