using HRM.BuildingBlocks.Application.Pagination;
using HRM.BuildingBlocks.Infrastructure.Extensions;
using HRM.Modules.Personnel.Api.Contracts;
using HRM.Modules.Personnel.Application.Abstractions;
using HRM.Modules.Personnel.Application.Commands.AssignManager;
using HRM.Modules.Personnel.Application.Commands.CreateEmployee;
using HRM.Modules.Personnel.Application.Commands.RemoveManager;
using HRM.Modules.Personnel.Application.Commands.TerminateEmployee;
using HRM.Modules.Personnel.Application.Commands.UpdateEmployee;
using HRM.Modules.Personnel.Application.Queries.GetDirectReports;
using HRM.Modules.Personnel.Application.Queries.GetEmployeeById;
using HRM.Modules.Personnel.Application.Queries.GetEmployees;
using HRM.Modules.Personnel.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HRM.Modules.Personnel.Api.Endpoints;

public static class EmployeeEndpoints
{
    public static IEndpointRouteBuilder MapEmployeeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/personnel/employees")
            .WithTags("Employees")
            .RequireAuthorization();

        group.MapPost("/", CreateEmployee)
            .WithName("CreateEmployee")
            .WithSummary("Create a new employee")
            .Produces<EmployeeResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/", GetEmployees)
            .WithName("GetEmployees")
            .WithSummary("Get employees")
            .Produces<PagedResult<EmployeeSummaryDto>>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}", GetEmployeeById)
            .WithName("GetEmployeeById")
            .WithSummary("Get employee by ID")
            .Produces<EmployeeDetailDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", UpdateEmployee)
            .WithName("UpdateEmployee")
            .WithSummary("Update employee personal information")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}/terminate", TerminateEmployee)
            .WithName("TerminateEmployee")
            .WithSummary("Terminate an employee")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/manager", AssignManager)
            .WithName("AssignManager")
            .WithSummary("Assign a manager to an employee")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}/manager", RemoveManager)
            .WithName("RemoveManager")
            .WithSummary("Remove manager assignment from an employee")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{managerId:guid}/direct-reports", GetDirectReports)
            .WithName("GetDirectReports")
            .WithSummary("Get direct reports")
            .Produces<PagedResult<EmployeeSummaryDto>>(StatusCodes.Status200OK);

        return app;
    }

    private static async Task<IResult> CreateEmployee(
        CreateEmployeeRequest request,
        ISender sender,
        IEmployeeRepository employeeRepository,
        CancellationToken cancellationToken)
    {
        var command = new CreateEmployeeCommand(
            EmployeeCode: request.EmployeeCode,
            FirstName: request.FirstName,
            LastName: request.LastName,
            Email: request.Email,
            HireDate: request.HireDate,
            Phone: request.Phone,
            DateOfBirth: request.DateOfBirth,
            ManagerId: request.ManagerId
        );

        var result = await sender.Send(command, cancellationToken);

        return await result.ToHttpResultAsync(async employeeId =>
        {
            var employee = await employeeRepository.GetByIdAsync(employeeId, cancellationToken);
            if (employee is null)
            {
                return Results.Problem(
                    detail: "Employee was created but could not be retrieved.",
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            var response = MapToResponse(employee);
            return Results.Created($"/api/personnel/employees/{employeeId}", response);
        });
    }

    private static async Task<IResult> UpdateEmployee(
        Guid id,
        UpdateEmployeeRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new UpdateEmployeeCommand(
            EmployeeId: id,
            FirstName: request.FirstName,
            LastName: request.LastName,
            Email: request.Email,
            Phone: request.Phone,
            DateOfBirth: request.DateOfBirth
        );

        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> TerminateEmployee(
        Guid id,
        TerminateEmployeeRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new TerminateEmployeeCommand(
            EmployeeId: id,
            TerminationDate: request.TerminationDate
        );

        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> AssignManager(
        Guid id,
        AssignManagerRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new AssignManagerCommand(
            EmployeeId: id,
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
        var command = new RemoveManagerCommand(EmployeeId: id);
        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetEmployees(
        ISender sender,
        CancellationToken cancellationToken,
        string? searchTerm = null,
        EmploymentStatus? status = null,
        Guid? companyId = null,
        Guid? departmentId = null,
        Guid? managerId = null,
        int pageNumber = 1,
        int pageSize = 20)
    {
        var query = new GetEmployeesQuery
        {
            SearchTerm = searchTerm,
            Status = status,
            CompanyId = companyId,
            DepartmentId = departmentId,
            ManagerId = managerId,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await sender.Send(query, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetEmployeeById(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetEmployeeByIdQuery { EmployeeId = id };
        var employee = await sender.Send(query, cancellationToken);

        if (employee is null)
        {
            return Results.NotFound(new
            {
                Code = "Employee.NotFound",
                Message = $"Employee with ID '{id}' was not found."
            });
        }

        return Results.Ok(employee);
    }

    private static async Task<IResult> GetDirectReports(
        Guid managerId,
        ISender sender,
        CancellationToken cancellationToken,
        int pageNumber = 1,
        int pageSize = 20)
    {
        var query = new GetDirectReportsQuery
        {
            ManagerId = managerId,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await sender.Send(query, cancellationToken);
        return Results.Ok(result);
    }

    private static EmployeeResponse MapToResponse(Employee employee) =>
        new(
            Id: employee.Id,
            EmployeeCode: employee.EmployeeCode,
            FirstName: employee.FirstName,
            LastName: employee.LastName,
            FullName: employee.FullName,
            Email: employee.Email,
            Phone: employee.Phone,
            DateOfBirth: employee.DateOfBirth,
            HireDate: employee.HireDate,
            TerminationDate: employee.TerminationDate,
            Status: employee.Status.ToString(),
            ManagerId: employee.ManagerId,
            PrimaryCompanyId: employee.PrimaryCompanyId,
            PrimaryDepartmentId: employee.PrimaryDepartmentId,
            PrimaryPositionId: employee.PrimaryPositionId,
            CreatedAtUtc: employee.CreatedAtUtc,
            ModifiedAtUtc: employee.ModifiedAtUtc
        );
}
