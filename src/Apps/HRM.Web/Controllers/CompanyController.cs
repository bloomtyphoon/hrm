using HRM.Web.Models;
using HRM.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Web.Controllers;

/// <summary>
/// Controller for company management.
/// Handles company creation, listing, and viewing.
/// </summary>
[Authorize]
public class CompanyController : Controller
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<CompanyController> _logger;

    public CompanyController(
        IApiClient apiClient,
        ILogger<CompanyController> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <summary>
    /// GET: /Company or /Company/Index
    /// Display list of companies.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(
        string? searchTerm = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var response = await _apiClient.GetCompaniesAsync(cancellationToken);

        var viewModel = new CompanyListViewModel
        {
            SearchTerm = searchTerm,
            StatusFilter = status
        };

        if (response.IsSuccess && response.Data != null)
        {
            var companies = response.Data;

            // Apply client-side filtering if needed
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                companies = companies
                    .Where(c =>
                        c.Code.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        c.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                companies = companies
                    .Where(c => c.Status.Equals(status, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            viewModel.Companies = companies;
        }
        else
        {
            _logger.LogError("Failed to get companies: {ErrorMessage}", response.ErrorMessage);
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to load companies";
            viewModel.Companies = [];
        }

        return View(viewModel);
    }

    /// <summary>
    /// GET: /Company/Create
    /// Display the company creation form.
    /// </summary>
    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateCompanyRequest());
    }

    /// <summary>
    /// POST: /Company/Create
    /// Process company creation form submission.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateCompanyRequest model,
        CancellationToken cancellationToken)
    {
        // Server-side validation
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Call API to create company
        var response = await _apiClient.CreateCompanyAsync(model, cancellationToken);

        if (response.IsSuccess && response.Data != null)
        {
            // Success - redirect to details page
            TempData["SuccessMessage"] = $"Company '{response.Data.Name}' created successfully!";
            return RedirectToAction(nameof(Details), new { id = response.Data.Id });
        }

        // Handle validation errors from API
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
            // General error message
            ModelState.AddModelError(string.Empty, response.ErrorMessage ?? "Failed to create company");
        }

        return View(model);
    }

    /// <summary>
    /// GET: /Company/Details/{id}
    /// Display company details.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var response = await _apiClient.GetCompanyByIdAsync(id, cancellationToken);

        if (response.IsSuccess && response.Data != null)
        {
            return View(response.Data);
        }

        TempData["ErrorMessage"] = response.ErrorMessage ?? "Company not found";
        return RedirectToAction(nameof(Index));
    }
}
