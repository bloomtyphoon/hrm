using HRM.BuildingBlocks.Application.Pagination;
using HRM.BuildingBlocks.Infrastructure.Extensions;
using HRM.Modules.Attendance.Api.Contracts;
using HRM.Modules.Attendance.Application.Commands.ApproveLeaveRequest;
using HRM.Modules.Attendance.Application.Commands.CancelLeaveRequest;
using HRM.Modules.Attendance.Application.Commands.CreateLeaveType;
using HRM.Modules.Attendance.Application.Commands.SubmitLeaveRequest;
using HRM.Modules.Attendance.Application.Commands.UpdateLeaveApprovalSettings;
using HRM.Modules.Attendance.Application.Queries.GetLeaveApprovalSettings;
using HRM.Modules.Attendance.Application.Queries.GetLeaveApprovalSteps;
using HRM.Modules.Attendance.Application.Queries.GetLeaveRequests;
using HRM.Modules.Attendance.Application.Queries.GetLeaveTypes;
using HRM.Modules.Attendance.Application.Queries.GetPendingApprovals;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HRM.Modules.Attendance.Api.Endpoints;

public static class LeaveEndpoints
{
    public static IEndpointRouteBuilder MapLeaveEndpoints(this IEndpointRouteBuilder app)
    {
        // Leave Types
        var types = app.MapGroup("/api/attendance/leave-types")
            .WithTags("Leave Types")
            .RequireAuthorization();

        types.MapGet("/", GetLeaveTypes)
            .WithName("GetLeaveTypes")
            .WithSummary("Get all leave types")
            .Produces<IReadOnlyList<LeaveTypeDto>>(StatusCodes.Status200OK);

        types.MapPost("/", CreateLeaveType)
            .WithName("CreateLeaveType")
            .WithSummary("Create a new leave type")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        // Leave Requests
        var requests = app.MapGroup("/api/attendance/leave-requests")
            .WithTags("Leave Requests")
            .RequireAuthorization();

        requests.MapGet("/", GetLeaveRequests)
            .WithName("GetLeaveRequests")
            .WithSummary("Get leave requests within scope")
            .Produces<PagedResult<LeaveRequestDto>>(StatusCodes.Status200OK);

        requests.MapPost("/", SubmitLeaveRequest)
            .WithName("SubmitLeaveRequest")
            .WithSummary("Submit a new leave request")
            .Produces<Guid>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        requests.MapPost("/{id:guid}/approve", ApproveLeaveRequest)
            .WithName("ApproveLeaveRequest")
            .WithSummary("Approve or reject a leave request")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        requests.MapPost("/{id:guid}/cancel", CancelLeaveRequest)
            .WithName("CancelLeaveRequest")
            .WithSummary("Cancel a leave request")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        requests.MapGet("/{id:guid}/approval-steps", GetLeaveApprovalSteps)
            .WithName("GetLeaveApprovalSteps")
            .WithSummary("Get approval steps for a leave request")
            .Produces<IReadOnlyList<LeaveApprovalStepDto>>(StatusCodes.Status200OK);

        requests.MapGet("/pending-approvals", GetPendingApprovals)
            .WithName("GetPendingApprovals")
            .WithSummary("Get leave requests pending the current user's approval")
            .Produces<PagedResult<PendingApprovalDto>>(StatusCodes.Status200OK);

        // Leave Approval Settings
        var settings = app.MapGroup("/api/attendance/leave-approval-settings")
            .WithTags("Leave Approval Settings")
            .RequireAuthorization();

        settings.MapGet("/", GetLeaveApprovalSettings)
            .WithName("GetLeaveApprovalSettings")
            .WithSummary("Get leave approval settings for the tenant")
            .Produces<LeaveApprovalSettingsDto>(StatusCodes.Status200OK);

        settings.MapPut("/", UpdateLeaveApprovalSettings)
            .WithName("UpdateLeaveApprovalSettings")
            .WithSummary("Update leave approval settings for the tenant")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return app;
    }

    private static async Task<IResult> GetLeaveTypes(
        ISender sender,
        CancellationToken cancellationToken,
        bool? isActive = null)
    {
        var query = new GetLeaveTypesQuery { IsActive = isActive };
        var result = await sender.Send(query, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> CreateLeaveType(
        CreateLeaveTypeRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new CreateLeaveTypeCommand(
            request.Name,
            request.DefaultDaysPerYear,
            request.IsPaid,
            request.Description);

        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult(id => Results.Created($"/api/attendance/leave-types/{id}", new { Id = id }));
    }

    private static async Task<IResult> GetLeaveRequests(
        ISender sender,
        CancellationToken cancellationToken,
        Guid? employeeId = null,
        string? status = null,
        int pageNumber = 1,
        int pageSize = 20)
    {
        var query = new GetLeaveRequestsQuery
        {
            EmployeeId = employeeId,
            Status = status,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        var result = await sender.Send(query, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> SubmitLeaveRequest(
        SubmitLeaveRequestRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new SubmitLeaveRequestCommand(
            request.LeaveTypeId,
            request.StartDate,
            request.EndDate,
            request.Reason);

        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult(id => Results.Created($"/api/attendance/leave-requests/{id}", new { Id = id }));
    }

    private static async Task<IResult> ApproveLeaveRequest(
        Guid id,
        ApproveLeaveRequestRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new ApproveLeaveRequestCommand(id, request.IsApproved, request.Notes);
        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> CancelLeaveRequest(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new CancelLeaveRequestCommand(id);
        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetLeaveApprovalSettings(
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetLeaveApprovalSettingsQuery();
        var result = await sender.Send(query, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> UpdateLeaveApprovalSettings(
        UpdateLeaveApprovalSettingsRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new UpdateLeaveApprovalSettingsCommand(
            request.RequiresApproval,
            request.MaxApprovalLevels,
            request.AutoApproveIfDaysLessThanOrEqual,
            request.AllowSelfCancel,
            request.NotifyOnDecision);

        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetLeaveApprovalSteps(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetLeaveApprovalStepsQuery(id);
        var result = await sender.Send(query, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetPendingApprovals(
        ISender sender,
        CancellationToken cancellationToken,
        int pageNumber = 1,
        int pageSize = 20)
    {
        var query = new GetPendingApprovalsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        var result = await sender.Send(query, cancellationToken);
        return Results.Ok(result);
    }
}
