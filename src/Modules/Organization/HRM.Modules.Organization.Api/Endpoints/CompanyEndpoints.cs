using HRM.BuildingBlocks.Infrastructure.Extensions;
using HRM.Modules.Organization.Api.Contracts;
using HRM.Modules.Organization.Application.Commands.ActivateCompany;
using HRM.Modules.Organization.Application.Commands.CreateCompany;
using HRM.Modules.Organization.Application.Commands.DeactivateCompany;
using HRM.Modules.Organization.Application.Commands.UpdateCompany;
using HRM.Modules.Organization.Application.DTOs;
using HRM.Modules.Organization.Application.Queries.GetCompanies;
using HRM.Modules.Organization.Application.Queries.GetCompanyById;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HRM.Modules.Organization.Api.Endpoints;

public static class CompanyEndpoints
{
    public static IEndpointRouteBuilder MapCompanyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/organization/companies")
            .WithTags("Companies")
            .RequireAuthorization();

        group.MapPost("/", CreateCompany)
            .WithName("CreateCompany")
            .WithSummary("Create a new company")
            .WithDescription("Create a new company with the specified code and name.")
            .Produces<CompanyResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapGet("/", GetCompanies)
            .WithName("GetCompanies")
            .WithSummary("Get all companies")
            .WithDescription("Retrieve a list of all companies. Use activeOnly=true to filter active companies only.")
            .Produces<IReadOnlyList<CompanyResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/{id:guid}", GetCompanyById)
            .WithName("GetCompanyById")
            .WithSummary("Get company by ID")
            .WithDescription("Retrieve a company by its ID.")
            .Produces<CompanyResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}", UpdateCompany)
            .WithName("UpdateCompany")
            .WithSummary("Update a company")
            .WithDescription("Update company name and tax ID.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/activate", ActivateCompany)
            .WithName("ActivateCompany")
            .WithSummary("Activate a company")
            .WithDescription("Set company status to Active.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPut("/{id:guid}/deactivate", DeactivateCompany)
            .WithName("DeactivateCompany")
            .WithSummary("Deactivate a company")
            .WithDescription("Set company status to Inactive.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> CreateCompany(
        CreateCompanyRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new CreateCompanyCommand(
            Code: request.Code,
            Name: request.Name,
            TaxId: request.TaxId
        );

        var result = await sender.Send(command, cancellationToken);

        return await result.ToHttpResultAsync(async companyId =>
        {
            var query = new GetCompanyByIdQuery(companyId);
            var company = await sender.Send(query, cancellationToken);

            if (company is null)
            {
                return Results.Problem(
                    detail: "Company was created but could not be retrieved.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }

            return Results.Created($"/api/organization/companies/{companyId}", MapToResponse(company));
        });
    }

    private static async Task<IResult> GetCompanies(
        bool? activeOnly,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetCompaniesQuery(ActiveOnly: activeOnly);
        var companies = await sender.Send(query, cancellationToken);

        var response = companies.Select(MapToResponse).ToList();
        return Results.Ok(response);
    }

    private static async Task<IResult> GetCompanyById(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetCompanyByIdQuery(id);
        var company = await sender.Send(query, cancellationToken);

        if (company is null)
        {
            return Results.NotFound(new
            {
                Code = "Company.NotFound",
                Message = $"Company with ID '{id}' was not found."
            });
        }

        return Results.Ok(MapToResponse(company));
    }

    private static async Task<IResult> UpdateCompany(
        Guid id,
        UpdateCompanyRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCompanyCommand(
            CompanyId: id,
            Name: request.Name,
            TaxId: request.TaxId
        );

        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> ActivateCompany(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new ActivateCompanyCommand(id);
        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> DeactivateCompany(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new DeactivateCompanyCommand(id);
        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static CompanyResponse MapToResponse(CompanyDto dto) =>
        new(
            Id: dto.Id,
            Code: dto.Code,
            Name: dto.Name,
            TaxId: dto.TaxId,
            Status: dto.Status,
            CreatedAtUtc: dto.CreatedAtUtc,
            ModifiedAtUtc: dto.ModifiedAtUtc
        );
}
