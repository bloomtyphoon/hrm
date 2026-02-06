using HRM.Web.Models;
using HRM.Web.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Web.Controllers;

/// <summary>
/// Controller for account management and session management.
/// </summary>
[Authorize]
public class AccountController : Controller
{
    private readonly IIdentityApiClient _identityClient;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IIdentityApiClient identityClient,
        ILogger<AccountController> logger)
    {
        _identityClient = identityClient;
        _logger = logger;
    }

    /// <summary>
    /// GET: /Account or /Account/Index
    /// Display paginated list of accounts with search and filter.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(
        string? searchTerm = null,
        string? status = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var response = await _identityClient.GetAccountsAsync(
            searchTerm,
            status,
            pageNumber,
            pageSize,
            cancellationToken);

        var viewModel = new AccountListViewModel
        {
            SearchTerm = searchTerm,
            StatusFilter = status,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        if (response.IsSuccess && response.Data != null)
        {
            viewModel.Accounts = response.Data;
        }
        else
        {
            _logger.LogError("Failed to get accounts: {ErrorMessage}", response.ErrorMessage);
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to load accounts";
            viewModel.Accounts = new PagedResult<AccountSummary>();
        }

        return View(viewModel);
    }

    /// <summary>
    /// GET: /Account/Detail/{id}
    /// Display account detail page with roles.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Detail(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        // Fetch account from list (no dedicated get-by-id endpoint)
        var accountResponse = await _identityClient.GetAccountsAsync(
            cancellationToken: cancellationToken);

        if (!accountResponse.IsSuccess || accountResponse.Data == null)
        {
            TempData["ErrorMessage"] = "Failed to load account.";
            return RedirectToAction(nameof(Index));
        }

        var account = accountResponse.Data.Items.FirstOrDefault(a => a.Id == id);
        if (account == null)
        {
            TempData["ErrorMessage"] = "Account not found.";
            return RedirectToAction(nameof(Index));
        }

        // Fetch roles assigned to this account
        var accountRolesResponse = await _identityClient.GetAccountRolesAsync(id, cancellationToken);
        var assignedRoles = accountRolesResponse.IsSuccess && accountRolesResponse.Data != null
            ? accountRolesResponse.Data
            : [];

        // Fetch all available roles
        var allRolesResponse = await _identityClient.GetRolesAsync(cancellationToken);
        var availableRoles = allRolesResponse.IsSuccess && allRolesResponse.Data != null
            ? allRolesResponse.Data.Where(r => r.IsActive).ToList()
            : [];

        var viewModel = new AccountDetailViewModel
        {
            Account = account,
            AssignedRoles = assignedRoles,
            AvailableRoles = availableRoles
        };

        return View(viewModel);
    }

    /// <summary>
    /// POST: /Account/AssignRoles/{id}
    /// Assign roles to an account.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignRoles(
        Guid id,
        List<Guid> roleIds,
        CancellationToken cancellationToken = default)
    {
        var request = new AssignRolesToAccountRequest
        {
            RoleIds = roleIds ?? []
        };

        var response = await _identityClient.AssignRolesToAccountAsync(id, request, cancellationToken);

        if (response.IsSuccess)
        {
            TempData["SuccessMessage"] = "Roles updated successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to update roles.";
        }

        return RedirectToAction(nameof(Detail), new { id });
    }

    /// <summary>
    /// POST: /Account/Activate/{id}
    /// Activate a pending account.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var response = await _identityClient.ActivateAccountAsync(id, cancellationToken);

        if (response.IsSuccess && response.Data != null)
        {
            TempData["SuccessMessage"] = $"Account '{response.Data.Username}' has been activated.";
        }
        else
        {
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to activate account.";
        }

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// GET: /Account/Register
    /// Display the account registration form.
    /// </summary>
    [HttpGet]
    public IActionResult Register()
    {
        return View(new RegisterAccountRequest());
    }

    /// <summary>
    /// POST: /Account/Register
    /// Process account registration form submission.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        RegisterAccountRequest model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var response = await _identityClient.RegisterAccountAsync(model, cancellationToken);

        if (response.IsSuccess && response.Data != null)
        {
            TempData["SuccessMessage"] = $"Account '{response.Data.Username}' registered successfully!";
            return RedirectToAction(nameof(Detail), new { id = response.Data.Id });
        }

        if (response.ValidationErrors != null)
        {
            foreach (var (field, errors) in response.ValidationErrors)
            {
                foreach (var error in errors)
                {
                    ModelState.AddModelError(field, error);
                }
            }
        }
        else
        {
            ModelState.AddModelError(string.Empty, response.ErrorMessage ?? "Registration failed");
        }

        return View(model);
    }

    /// <summary>
    /// GET: /Account/Sessions
    /// Display active sessions for current user.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Sessions(
        CancellationToken cancellationToken = default)
    {
        var response = await _identityClient.GetActiveSessionsAsync(cancellationToken);

        var viewModel = new SessionListViewModel();

        if (response.IsSuccess && response.Data != null)
        {
            viewModel.Sessions = response.Data;
        }
        else
        {
            _logger.LogError("Failed to get sessions: {ErrorMessage}", response.ErrorMessage);
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to load sessions.";
        }

        return View(viewModel);
    }

    /// <summary>
    /// POST: /Account/RevokeSession/{sessionId}
    /// Revoke a specific session (logout from specific device).
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeSession(
        Guid sessionId,
        CancellationToken cancellationToken = default)
    {
        var response = await _identityClient.RevokeSessionAsync(sessionId, cancellationToken);

        if (response.IsSuccess)
        {
            TempData["SuccessMessage"] = "Session has been revoked.";
        }
        else
        {
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to revoke session.";
        }

        return RedirectToAction(nameof(Sessions));
    }

    /// <summary>
    /// POST: /Account/RevokeAllSessions
    /// Revoke all sessions except current.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeAllSessions(
        CancellationToken cancellationToken = default)
    {
        var response = await _identityClient.RevokeAllSessionsExceptCurrentAsync(cancellationToken);

        if (response.IsSuccess && response.Data != null)
        {
            TempData["SuccessMessage"] = response.Data.Message;
        }
        else
        {
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to revoke sessions.";
        }

        return RedirectToAction(nameof(Sessions));
    }
}
