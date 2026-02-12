using HRM.BuildingBlocks.Application.Pagination;
using HRM.Modules.Identity.Domain.Enums;
using HRM.BuildingBlocks.Infrastructure.Extensions;
using HRM.Modules.Identity.Api.Contracts;
using HRM.Modules.Identity.Application.Commands.ActivateAccount;
using HRM.Modules.Identity.Application.Commands.AssignRolesToAccount;
using HRM.Modules.Identity.Application.Commands.ChangePassword;
using HRM.Modules.Identity.Application.Commands.DeactivateAccount;
using HRM.Modules.Identity.Application.Commands.RegisterAccount;
using HRM.Modules.Identity.Application.Commands.DisableTwoFactor;
using HRM.Modules.Identity.Application.Commands.EnableTwoFactor;
using HRM.Modules.Identity.Application.Commands.RemoveRolesFromAccount;
using HRM.Modules.Identity.Application.Commands.SuspendAccount;
using HRM.Modules.Identity.Application.Commands.UnlockAccount;
using HRM.Modules.Identity.Application.Commands.UpdateProfile;
using HRM.Modules.Identity.Application.Queries.GetAccountRoles;
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

        group.MapGet("/{id:guid}", GetAccountById)
            .WithName("GetAccountById")
            .WithSummary("Get account by ID")
            .WithDescription("Retrieve a single account by ID. Requires Identity.Account.View permission.")
            .Produces<AccountResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/suspend", SuspendAccount)
            .WithName("SuspendAccount")
            .WithSummary("Suspend an active account")
            .WithDescription("Change account status to Suspended. Requires Identity.Account.Update permission.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/deactivate", DeactivateAccount)
            .WithName("DeactivateAccount")
            .WithSummary("Deactivate an account")
            .WithDescription("Change account status to Deactivated. Requires Identity.Account.Update permission.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id:guid}/profile", UpdateProfile)
            .WithName("UpdateProfile")
            .WithSummary("Update account profile")
            .WithDescription("Update account profile information (fullName, phoneNumber).")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/change-password", ChangePassword)
            .WithName("ChangePassword")
            .WithSummary("Change account password")
            .WithDescription("Change account password. Requires current password for self-change, or IsAdminReset=true for admin reset.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/roles", GetAccountRoles)
            .WithName("GetAccountRoles")
            .WithSummary("Get account's assigned roles")
            .WithDescription("Returns all roles assigned to the account.")
            .Produces<List<AccountRoleDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/roles", AssignRoles)
            .WithName("AssignRolesToAccount")
            .WithSummary("Assign roles to account")
            .WithDescription("Assign one or more roles to an account. Skips already assigned roles.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id:guid}/roles", RemoveRoles)
            .WithName("RemoveRolesFromAccount")
            .WithSummary("Remove roles from account")
            .WithDescription("Remove one or more roles from an account.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/enable-2fa", EnableTwoFactor)
            .WithName("EnableTwoFactor")
            .WithSummary("Enable two-factor authentication")
            .WithDescription("Enable TOTP-based two-factor authentication. Returns the secret key for authenticator app setup.")
            .Produces<Contracts.EnableTwoFactorResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/disable-2fa", DisableTwoFactor)
            .WithName("DisableTwoFactor")
            .WithSummary("Disable two-factor authentication")
            .WithDescription("Disable two-factor authentication for the account.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/unlock", UnlockAccount)
            .WithName("UnlockAccount")
            .WithSummary("Unlock a locked account")
            .WithDescription("Force unlock an account that is locked due to failed login attempts. Admin operation.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

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
        Guid? companyId = null,
        bool allCompanies = false,
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
            CompanyId = companyId,
            AllCompanies = allCompanies,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await sender.Send(query, cancellationToken);
        return Results.Ok(result);
    }

    private static async Task<IResult> GetAccountById(
        Guid id,
        IAccountRepository accountRepository,
        CancellationToken cancellationToken)
    {
        var account = await accountRepository.GetByIdAsync(id, cancellationToken);

        if (account is null)
        {
            return Results.NotFound();
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

    private static async Task<IResult> SuspendAccount(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new SuspendAccountCommand(AccountId: id);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return Results.NoContent();
        }

        return result.ToHttpResult();
    }

    private static async Task<IResult> DeactivateAccount(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new DeactivateAccountCommand(AccountId: id);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return Results.NoContent();
        }

        return result.ToHttpResult();
    }

    private static async Task<IResult> UpdateProfile(
        Guid id,
        UpdateProfileRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new UpdateProfileCommand(
            AccountId: id,
            FullName: request.FullName,
            PhoneNumber: request.PhoneNumber
        );

        var result = await sender.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return Results.NoContent();
        }

        return result.ToHttpResult();
    }

    private static async Task<IResult> ChangePassword(
        Guid id,
        ChangePasswordRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new ChangePasswordCommand(
            AccountId: id,
            CurrentPassword: request.CurrentPassword,
            NewPassword: request.NewPassword,
            IsAdminReset: request.IsAdminReset
        );

        var result = await sender.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return Results.NoContent();
        }

        return result.ToHttpResult();
    }

    private static async Task<IResult> GetAccountRoles(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var query = new GetAccountRolesQuery(id);
        var result = await sender.Send(query, cancellationToken);

        return result.ToHttpResult(roles => Results.Ok(roles));
    }

    private static async Task<IResult> AssignRoles(
        Guid id,
        AssignRolesRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new AssignRolesToAccountCommand(
            AccountId: id,
            RoleIds: request.RoleIds
        );

        var result = await sender.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return Results.NoContent();
        }

        return result.ToHttpResult();
    }

    private static async Task<IResult> RemoveRoles(
        Guid id,
        [Microsoft.AspNetCore.Mvc.FromBody] AssignRolesRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new RemoveRolesFromAccountCommand(
            AccountId: id,
            RoleIds: request.RoleIds
        );

        var result = await sender.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return Results.NoContent();
        }

        return result.ToHttpResult();
    }

    private static async Task<IResult> EnableTwoFactor(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new EnableTwoFactorCommand(AccountId: id);
        var result = await sender.Send(command, cancellationToken);

        return result.ToHttpResult(response =>
            Results.Ok(new Contracts.EnableTwoFactorResponse(SecretKey: response.SecretKey)));
    }

    private static async Task<IResult> DisableTwoFactor(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new DisableTwoFactorCommand(AccountId: id);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return Results.NoContent();
        }

        return result.ToHttpResult();
    }

    private static async Task<IResult> UnlockAccount(
        Guid id,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var command = new UnlockAccountCommand(AccountId: id);
        var result = await sender.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            return Results.NoContent();
        }

        return result.ToHttpResult();
    }
}
