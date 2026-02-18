using HRM.Web.Models;
using HRM.Web.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Web.Controllers;

/// <summary>
/// Controller for position management.
/// Uses CompanyContext to filter positions by the selected company.
/// </summary>
[Authorize]
public class PositionController : Controller
{
    private readonly IOrganizationApiClient _organizationClient;
    private readonly ICompanyContext _companyContext;
    private readonly ILogger<PositionController> _logger;

    public PositionController(
        IOrganizationApiClient organizationClient,
        ICompanyContext companyContext,
        ILogger<PositionController> logger)
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

        // Positions require a specific company to be selected
        if (companyId is null || _companyContext.IsAllCompanies)
        {
            return View(new PositionListViewModel
            {
                SearchTerm = searchTerm,
                StatusFilter = status,
                RequiresCompanySelection = true
            });
        }

        var response = await _organizationClient.GetPositionsByCompanyAsync(
            companyId.Value, cancellationToken);

        var viewModel = new PositionListViewModel
        {
            SearchTerm = searchTerm,
            StatusFilter = status,
            CompanyId = companyId
        };

        if (response.IsSuccess && response.Data != null)
        {
            var positions = response.Data;

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                positions = positions
                    .Where(p =>
                        p.Code.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        p.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                positions = positions
                    .Where(p => p.Status.Equals(status, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            viewModel.Positions = positions;
        }
        else
        {
            _logger.LogError("Failed to get positions: {ErrorMessage}", response.ErrorMessage);
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to load positions";
            viewModel.Positions = [];
        }

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var response = await _organizationClient.GetPositionByIdAsync(id, cancellationToken);

        if (response.IsSuccess && response.Data != null)
        {
            return View(response.Data);
        }

        TempData["ErrorMessage"] = response.ErrorMessage ?? "Position not found";
        return RedirectToAction(nameof(Index));
    }
}
