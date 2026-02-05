using HRM.BuildingBlocks.Infrastructure.Extensions;
using HRM.Modules.Organization.Api.Contracts;
using HRM.Modules.Organization.Application.Commands.CreateCompany;
using HRM.Modules.Organization.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HRM.Modules.Organization.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for Company operations.
/// Implements RESTful API for company management.
///
/// Architecture:
/// - Minimal API: Lightweight alternative to Controllers
/// - CQRS: Commands via MediatR (CreateCompany)
/// - Result Pattern: Type-safe error handling with DomainError
/// - Pure DDD: Domain errors mapped to HTTP in API layer
/// - DTO Mapping: Request DTOs -> Commands, Entities -> Response DTOs
///
/// Endpoints:
/// 1. POST /api/organization/companies
///    - Create new company
///    - Returns: 201 Created with CompanyResponse
///
/// 2. GET /api/organization/companies
///    - Get all companies
///    - Returns: 200 OK with list of CompanyResponse
///
/// Authorization Architecture:
/// - Single Source of Truth: RouteSecurityMap.xml
/// - .RequireAuthorization() only for OpenAPI docs (lock icon) and basic auth check
/// - Actual permission checks: RoutePermissionMiddleware (reads from XML)
/// </summary>
public static class CompanyEndpoints
{
    /// <summary>
    /// Map company endpoints to route builder.
    /// </summary>
    public static IEndpointRouteBuilder MapCompanyEndpoints(this IEndpointRouteBuilder app)
    {
        // Create route group: /api/organization/companies
        var group = app.MapGroup("/api/organization/companies")
            .WithTags("Companies")
            .RequireAuthorization();

        // 1. Create company
        group.MapPost("/", CreateCompany)
            .WithName("CreateCompany")
            .WithSummary("Create a new company")
            .WithDescription("Create a new company with the specified code and name.")
            .Produces<CompanyResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // 2. Get all companies
        group.MapGet("/", GetCompanies)
            .WithName("GetCompanies")
            .WithSummary("Get all companies")
            .WithDescription("Retrieve a list of all companies.")
            .Produces<IReadOnlyList<CompanyResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        // 3. Get company by ID
        group.MapGet("/{id:guid}", GetCompanyById)
            .WithName("GetCompanyById")
            .WithSummary("Get company by ID")
            .WithDescription("Retrieve a company by its ID.")
            .Produces<CompanyResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    /// <summary>
    /// POST /api/organization/companies
    /// Create a new company.
    /// </summary>
    private static async Task<IResult> CreateCompany(
        CreateCompanyRequest request,
        ISender sender,
        ICompanyRepository companyRepository,
        CancellationToken cancellationToken)
    {
        // Map request DTO to command
        var command = new CreateCompanyCommand(
            Code: request.Code,
            Name: request.Name,
            TaxId: request.TaxId
        );

        // Execute command via MediatR
        var result = await sender.Send(command, cancellationToken);

        // Map Result<Guid> to HTTP response
        return await result.ToHttpResultAsync(async companyId =>
        {
            // Retrieve created company
            var company = await companyRepository.GetByIdAsync(companyId, cancellationToken);

            if (company is null)
            {
                return Results.Problem(
                    detail: "Company was created but could not be retrieved.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }

            // Map entity to response DTO
            var response = new CompanyResponse(
                Id: company.Id,
                Code: company.Code,
                Name: company.Name,
                TaxId: company.TaxId,
                Status: company.Status.ToString(),
                CreatedAtUtc: company.CreatedAtUtc,
                ModifiedAtUtc: company.ModifiedAtUtc
            );

            // Return 201 Created with Location header
            return Results.Created($"/api/organization/companies/{companyId}", response);
        });
    }

    /// <summary>
    /// GET /api/organization/companies
    /// Get all companies.
    /// </summary>
    private static async Task<IResult> GetCompanies(
        ICompanyRepository companyRepository,
        CancellationToken cancellationToken)
    {
        var companies = await companyRepository.GetAllAsync(cancellationToken);

        var response = companies.Select(company => new CompanyResponse(
            Id: company.Id,
            Code: company.Code,
            Name: company.Name,
            TaxId: company.TaxId,
            Status: company.Status.ToString(),
            CreatedAtUtc: company.CreatedAtUtc,
            ModifiedAtUtc: company.ModifiedAtUtc
        )).ToList();

        return Results.Ok(response);
    }

    /// <summary>
    /// GET /api/organization/companies/{id}
    /// Get company by ID.
    /// </summary>
    private static async Task<IResult> GetCompanyById(
        Guid id,
        ICompanyRepository companyRepository,
        CancellationToken cancellationToken)
    {
        var company = await companyRepository.GetByIdAsync(id, cancellationToken);

        if (company is null)
        {
            return Results.NotFound(new
            {
                Code = "Company.NotFound",
                Message = $"Company with ID '{id}' was not found."
            });
        }

        var response = new CompanyResponse(
            Id: company.Id,
            Code: company.Code,
            Name: company.Name,
            TaxId: company.TaxId,
            Status: company.Status.ToString(),
            CreatedAtUtc: company.CreatedAtUtc,
            ModifiedAtUtc: company.ModifiedAtUtc
        );

        return Results.Ok(response);
    }
}
