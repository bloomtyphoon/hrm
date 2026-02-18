using HRM.BuildingBlocks.Application.Pagination;
using HRM.BuildingBlocks.Infrastructure.Extensions;
using HRM.Modules.Personnel.Api.Contracts;
using HRM.Modules.Personnel.Application.Abstractions;
using HRM.Modules.Personnel.Application.Commands.CreateEmployee;
using HRM.Modules.Personnel.Application.Queries.GetDirectReports;
using HRM.Modules.Personnel.Application.Queries.GetEmployeeById;
using HRM.Modules.Personnel.Application.Queries.GetEmployees;
using HRM.Modules.Personnel.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HRM.Modules.Personnel.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for Employee operations.
/// </summary>
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
            .WithDescription("Create a new employee in the system.")
            .Produces<EmployeeResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/", GetEmployees)
            .WithName("GetEmployees")
            .WithSummary("Get employees")
            .WithDescription("Retrieve paginated list of employees with optional filtering.")
            .Produces<PagedResult<EmployeeSummaryDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/{id:guid}", GetEmployeeById)
            .WithName("GetEmployeeById")
            .WithSummary("Get employee by ID")
            .WithDescription("Retrieve an employee by their ID.")
            .Produces<EmployeeDetailDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{managerId:guid}/direct-reports", GetDirectReports)
            .WithName("GetDirectReports")
            .WithSummary("Get direct reports")
            .WithDescription("Retrieve paginated direct reports for a manager.")
            .Produces<PagedResult<EmployeeSummaryDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

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
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }

            var response = MapToResponse(employee);
            return Results.Created($"/api/personnel/employees/{employeeId}", response);
        });
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
