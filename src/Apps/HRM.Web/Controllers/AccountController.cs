using HRM.Web.Models;
using HRM.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Web.Controllers;

/// <summary>
/// Controller for account management.
/// Handles account registration and related operations.
/// </summary>
[Authorize]
public class AccountController : Controller
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        IApiClient apiClient,
        ILogger<AccountController> logger)
    {
        _apiClient = apiClient;
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

        var response = await _apiClient.GetAccountsAsync(
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

        var response = await _apiClient.RegisterAccountAsync(model, cancellationToken);

        if (response.IsSuccess && response.Data != null)
        {
            TempData["SuccessMessage"] = $"Account '{response.Data.Username}' registered successfully!";
            TempData["AccountId"] = response.Data.Id;
            return RedirectToAction(nameof(RegisterSuccess));
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
    /// GET: /Account/RegisterSuccess
    /// Display registration success page.
    /// </summary>
    [HttpGet]
    public IActionResult RegisterSuccess()
    {
        if (TempData["SuccessMessage"] == null)
        {
            return RedirectToAction(nameof(Register));
        }

        ViewBag.SuccessMessage = TempData["SuccessMessage"];
        ViewBag.AccountId = TempData["AccountId"];
        return View();
    }
}
