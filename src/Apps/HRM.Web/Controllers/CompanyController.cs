using HRM.Web.Models;
using HRM.Web.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Web.Controllers;

[Authorize]
public class CompanyController : Controller
{
    private readonly IOrganizationApiClient _organizationClient;
    private readonly ILogger<CompanyController> _logger;

    public CompanyController(
        IOrganizationApiClient organizationClient,
        ILogger<CompanyController> logger)
    {
        _organizationClient = organizationClient;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? searchTerm = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var response = await _organizationClient.GetCompaniesAsync(cancellationToken);

        var viewModel = new CompanyListViewModel
        {
            SearchTerm = searchTerm,
            StatusFilter = status
        };

        if (response.IsSuccess && response.Data != null)
        {
            var companies = response.Data;

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

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateCompanyRequest());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateCompanyRequest model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var response = await _organizationClient.CreateCompanyAsync(model, cancellationToken);

        if (response.IsSuccess && response.Data != null)
        {
            TempData["SuccessMessage"] = $"Company '{response.Data.Name}' created successfully!";
            return RedirectToAction(nameof(Details), new { id = response.Data.Id });
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
            ModelState.AddModelError(string.Empty, response.ErrorMessage ?? "Failed to create company");
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var response = await _organizationClient.GetCompanyByIdAsync(id, cancellationToken);

        if (response.IsSuccess && response.Data != null)
        {
            return View(response.Data);
        }

        TempData["ErrorMessage"] = response.ErrorMessage ?? "Company not found";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var response = await _organizationClient.GetCompanyByIdAsync(id, cancellationToken);

        if (!response.IsSuccess || response.Data is null)
        {
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Company not found";
            return RedirectToAction(nameof(Index));
        }

        var company = response.Data;
        var model = new EditCompanyFormModel
        {
            Id = company.Id,
            Code = company.Code,
            Name = company.Name,
            TaxId = company.TaxId,
            Status = company.Status
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        EditCompanyFormModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var response = await _organizationClient.UpdateCompanyAsync(
            model.Id, model.Name, model.TaxId, cancellationToken);

        if (response.IsSuccess)
        {
            TempData["SuccessMessage"] = "Company updated successfully!";
            return RedirectToAction(nameof(Details), new { id = model.Id });
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
            ModelState.AddModelError(string.Empty, response.ErrorMessage ?? "Failed to update company");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var response = await _organizationClient.ActivateCompanyAsync(id, cancellationToken);
        TempData[response.IsSuccess ? "SuccessMessage" : "ErrorMessage"] =
            response.IsSuccess ? "Company activated successfully!" : (response.ErrorMessage ?? "Failed to activate company");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var response = await _organizationClient.DeactivateCompanyAsync(id, cancellationToken);
        TempData[response.IsSuccess ? "SuccessMessage" : "ErrorMessage"] =
            response.IsSuccess ? "Company deactivated successfully!" : (response.ErrorMessage ?? "Failed to deactivate company");
        return RedirectToAction(nameof(Details), new { id });
    }
}
