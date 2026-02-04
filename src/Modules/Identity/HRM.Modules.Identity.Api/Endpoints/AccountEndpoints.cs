using HRM.BuildingBlocks.Application.Pagination;
using HRM.Modules.Identity.Domain.Enums;
using HRM.BuildingBlocks.Infrastructure.Extensions;
using HRM.Modules.Identity.Api.Contracts;
using HRM.Modules.Identity.Application.Commands.ActivateAccount;
using HRM.Modules.Identity.Application.Commands.RegisterAccount;
using HRM.Modules.Identity.Application.Queries.GetAccounts;
using HRM.Modules.Identity.Domain.Repositories;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace HRM.Modules.Identity.Api.Endpoints;

/// <summary>
/// Minimal API endpoints for Account operations.
/// </summary>
public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/identity/accounts")
            .WithTags("Accounts")
            .RequireAuthorization();

        group.MapPost("/register", RegisterAccount)
            .WithName("RegisterAccount")
            .WithSummary("Register a new account")
            .WithDescription("Create a new account in Pending status. Requires Identity.Account.Create permission.")
            .Produces<AccountResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/activate", ActivateAccount)
            .WithName("ActivateAccount")
            .WithSummary("Activate a pending account")
            .WithDescription("Change account status from Pending to Active. Requires Identity.Account.Update permission.")
            .Produces<AccountResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/", GetAccounts)
            .WithName("GetAccounts")
            .WithSummary("Get paginated list of accounts")
            .WithDescription("Retrieve accounts with search, filter, and pagination. Requires Identity.Account.View permission.")
            .Produces<PagedResult<AccountSummaryDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        return app;
    }

    private static async Task<IResult> RegisterAccount(
        RegisterAccountRequest request,
        ISender sender,
        IAccountRepository accountRepository,
        CancellationToken cancellationToken)
    {
        var command = new RegisterAccountCommand(
            Username: request.Username,
            Email: request.Email,
            Password: request.Password,
            FullName: request.FullName,
            PhoneNumber: request.PhoneNumber
        );

        var result = await sender.Send(command, cancellationToken);

        return await result.ToHttpResultAsync(async accountId =>
        {
            var account = await accountRepository.GetByIdAsync(accountId, cancellationToken);

            if (account is null)
            {
                return Results.Problem(
                    detail: "Account was created but could not be retrieved.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }

            var response = new AccountResponse(
                Id: account.Id,
                Username: account.Username,
                Email: account.Email,
                FullName: account.FullName,
                PhoneNumber: account.PhoneNumber,
                Status: account.Status.ToString(),
                AccountType: account.AccountType.ToString(),
                IsTwoFactorEnabled: account.IsTwoFactorEnabled,
                ActivatedAtUtc: account.ActivatedAtUtc,
                LastLoginAtUtc: account.LastLoginAtUtc,
                CreatedAtUtc: account.CreatedAtUtc,
                ModifiedAtUtc: account.ModifiedAtUtc
            );

            return Results.Created($"/api/identity/accounts/{accountId}", response);
        });
    }

    private static async Task<IResult> ActivateAccount(
        Guid id,
        ISender sender,
        IAccountRepository accountRepository,
        CancellationToken cancellationToken)
    {
        var command = new ActivateAccountCommand(AccountId: id);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            var account = await accountRepository.GetByIdAsync(id, cancellationToken);

            if (account is null)
            {
                return Results.Problem(
                    detail: "Account was activated but could not be retrieved.",
                    statusCode: StatusCodes.Status500InternalServerError
                );
            }

            var response = new AccountResponse(
                Id: account.Id,
                Username: account.Username,
                Email: account.Email,
                FullName: account.FullName,
                PhoneNumber: account.PhoneNumber,
                Status: account.Status.ToString(),
                AccountType: account.AccountType.ToString(),
                IsTwoFactorEnabled: account.IsTwoFactorEnabled,
                ActivatedAtUtc: account.ActivatedAtUtc,
                LastLoginAtUtc: account.LastLoginAtUtc,
                CreatedAtUtc: account.CreatedAtUtc,
                ModifiedAtUtc: account.ModifiedAtUtc
            );

            return Results.Ok(response);
        }

        return result.ToHttpResult();
    }

    private static async Task<IResult> GetAccounts(
        ISender sender,
        string? searchTerm = null,
        AccountStatus? status = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var query = new GetAccountsQuery
        {
            SearchTerm = searchTerm,
            Status = status,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await sender.Send(query, cancellationToken);
        return Results.Ok(result);
    }
}
