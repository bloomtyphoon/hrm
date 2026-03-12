using HRM.Web.Models;
using HRM.Web.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Web.Controllers;

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

        if (!response.IsSuccess || response.Data is null)
        {
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Position not found";
            return RedirectToAction(nameof(Index));
        }

        var pos = response.Data;
        var viewModel = new PositionDetailViewModel { Position = pos };

        if (pos.DepartmentId.HasValue)
        {
            var dept = await _organizationClient.GetDepartmentByIdAsync(pos.DepartmentId.Value, cancellationToken);
            if (dept.IsSuccess && dept.Data != null)
                viewModel.DepartmentName = dept.Data.Name;
        }

        // Load available departments for move
        var departments = await _organizationClient.GetDepartmentsByCompanyAsync(pos.CompanyId, cancellationToken);
        if (departments.IsSuccess && departments.Data != null)
            viewModel.AvailableDepartments = departments.Data;

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var companyId = _companyContext.SelectedCompanyId;

        if (companyId is null || _companyContext.IsAllCompanies)
        {
            TempData["ErrorMessage"] = "Please select a specific company to create a position.";
            return RedirectToAction(nameof(Index));
        }

        var departmentsResponse = await _organizationClient.GetDepartmentsByCompanyAsync(
            companyId.Value, cancellationToken);

        var viewModel = new CreatePositionViewModel
        {
            Form = new CreatePositionFormModel { CompanyId = companyId.Value },
            AvailableDepartments = departmentsResponse.IsSuccess ? departmentsResponse.Data ?? [] : []
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreatePositionViewModel viewModel,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var deptResponse = await _organizationClient.GetDepartmentsByCompanyAsync(
                viewModel.Form.CompanyId, cancellationToken);
            viewModel.AvailableDepartments = deptResponse.IsSuccess ? deptResponse.Data ?? [] : [];
            return View(viewModel);
        }

        var response = await _organizationClient.CreatePositionAsync(
            viewModel.Form.CompanyId,
            viewModel.Form.Code,
            viewModel.Form.Title,
            viewModel.Form.PositionLevel,
            viewModel.Form.IsManagement,
            viewModel.Form.DepartmentId,
            viewModel.Form.Description,
            viewModel.Form.MaxHeadcount,
            cancellationToken);

        if (response.IsSuccess && response.Data != null)
        {
            TempData["SuccessMessage"] = $"Position '{response.Data.Title}' created successfully!";
            return RedirectToAction(nameof(Details), new { id = response.Data.Id });
        }

        if (response.ValidationErrors != null)
        {
            foreach (var (field, errors) in response.ValidationErrors)
            {
                foreach (var error in errors)
                {
                    ModelState.AddModelError($"Form.{field}", error);
                }
            }
        }
        else
        {
            ModelState.AddModelError(string.Empty, response.ErrorMessage ?? "Failed to create position");
        }

        var departmentsResponse = await _organizationClient.GetDepartmentsByCompanyAsync(
            viewModel.Form.CompanyId, cancellationToken);
        viewModel.AvailableDepartments = departmentsResponse.IsSuccess ? departmentsResponse.Data ?? [] : [];
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var response = await _organizationClient.GetPositionByIdAsync(id, cancellationToken);

        if (!response.IsSuccess || response.Data is null)
        {
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Position not found";
            return RedirectToAction(nameof(Index));
        }

        var pos = response.Data;
        var model = new EditPositionFormModel
        {
            Id = pos.Id,
            CompanyId = pos.CompanyId,
            Code = pos.Code,
            Title = pos.Title,
            Description = pos.Description,
            PositionLevel = pos.PositionLevel,
            IsManagement = pos.IsManagement,
            MaxHeadcount = pos.MaxHeadcount,
            Status = pos.Status
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        EditPositionFormModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var response = await _organizationClient.UpdatePositionAsync(
            model.Id,
            model.Title,
            model.PositionLevel,
            model.IsManagement,
            model.Description,
            model.MaxHeadcount,
            cancellationToken);

        if (response.IsSuccess)
        {
            TempData["SuccessMessage"] = "Position updated successfully!";
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
            ModelState.AddModelError(string.Empty, response.ErrorMessage ?? "Failed to update position");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var response = await _organizationClient.ActivatePositionAsync(id, cancellationToken);
        TempData[response.IsSuccess ? "SuccessMessage" : "ErrorMessage"] =
            response.IsSuccess ? "Position activated successfully!" : (response.ErrorMessage ?? "Failed to activate position");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var response = await _organizationClient.DeactivatePositionAsync(id, cancellationToken);
        TempData[response.IsSuccess ? "SuccessMessage" : "ErrorMessage"] =
            response.IsSuccess ? "Position deactivated successfully!" : (response.ErrorMessage ?? "Failed to deactivate position");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(Guid id, CancellationToken cancellationToken)
    {
        var response = await _organizationClient.ClosePositionAsync(id, cancellationToken);
        TempData[response.IsSuccess ? "SuccessMessage" : "ErrorMessage"] =
            response.IsSuccess ? "Position closed successfully!" : (response.ErrorMessage ?? "Failed to close position");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Move(Guid id, Guid? departmentId, CancellationToken cancellationToken)
    {
        var response = await _organizationClient.MovePositionAsync(id, departmentId, cancellationToken);
        TempData[response.IsSuccess ? "SuccessMessage" : "ErrorMessage"] =
            response.IsSuccess ? "Position moved successfully!" : (response.ErrorMessage ?? "Failed to move position");
        return RedirectToAction(nameof(Details), new { id });
    }
}
