using HRM.Web.Models;
using HRM.Web.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Web.Controllers;

/// <summary>
/// Controller for department management.
/// Uses CompanyContext to filter departments by the selected company.
/// </summary>
[Authorize]
public class DepartmentController : Controller
{
    private readonly IOrganizationApiClient _organizationClient;
    private readonly ICompanyContext _companyContext;
    private readonly ILogger<DepartmentController> _logger;

    public DepartmentController(
        IOrganizationApiClient organizationClient,
        ICompanyContext companyContext,
        ILogger<DepartmentController> logger)
    {
        _organizationClient = organizationClient;
        _companyContext = companyContext;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? searchTerm = null,
        string? status = null,
        CancellationToken cancellationToken = default)
    {
        var companyId = _companyContext.SelectedCompanyId;

        // Departments require a specific company to be selected
        if (companyId is null || _companyContext.IsAllCompanies)
        {
            return View(new DepartmentListViewModel
            {
                SearchTerm = searchTerm,
                StatusFilter = status,
                RequiresCompanySelection = true
            });
        }

        var response = await _organizationClient.GetDepartmentsByCompanyAsync(
            companyId.Value, cancellationToken);

        var viewModel = new DepartmentListViewModel
        {
            SearchTerm = searchTerm,
            StatusFilter = status,
            CompanyId = companyId
        };

        if (response.IsSuccess && response.Data != null)
        {
            var departments = response.Data;

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                departments = departments
                    .Where(d =>
                        d.Code.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        d.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                departments = departments
                    .Where(d => d.Status.Equals(status, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            viewModel.Departments = departments;
        }
        else
        {
            _logger.LogError("Failed to get departments: {ErrorMessage}", response.ErrorMessage);
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to load departments";
            viewModel.Departments = [];
        }

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var response = await _organizationClient.GetDepartmentByIdAsync(id, cancellationToken);

        if (response.IsSuccess && response.Data != null)
        {
            return View(response.Data);
        }

        TempData["ErrorMessage"] = response.ErrorMessage ?? "Department not found";
        return RedirectToAction(nameof(Index));
    }
}
