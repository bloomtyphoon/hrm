using HRM.Web.Models;
using HRM.Web.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Web.Controllers;

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

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var companyId = _companyContext.SelectedCompanyId;

        if (companyId is null || _companyContext.IsAllCompanies)
        {
            TempData["ErrorMessage"] = "Please select a specific company to create a department.";
            return RedirectToAction(nameof(Index));
        }

        var departmentsResponse = await _organizationClient.GetDepartmentsByCompanyAsync(
            companyId.Value, cancellationToken);

        var viewModel = new CreateDepartmentViewModel
        {
            Form = new CreateDepartmentFormModel { CompanyId = companyId.Value },
            AvailableParentDepartments = departmentsResponse.IsSuccess ? departmentsResponse.Data ?? [] : []
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateDepartmentViewModel viewModel,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var departmentsResponse = await _organizationClient.GetDepartmentsByCompanyAsync(
                viewModel.Form.CompanyId, cancellationToken);
            viewModel.AvailableParentDepartments = departmentsResponse.IsSuccess ? departmentsResponse.Data ?? [] : [];
            return View(viewModel);
        }

        var response = await _organizationClient.CreateDepartmentAsync(
            viewModel.Form.CompanyId,
            viewModel.Form.Code,
            viewModel.Form.Name,
            viewModel.Form.ParentDepartmentId,
            cancellationToken);

        if (response.IsSuccess && response.Data != null)
        {
            TempData["SuccessMessage"] = $"Department '{response.Data.Name}' created successfully!";
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
            ModelState.AddModelError(string.Empty, response.ErrorMessage ?? "Failed to create department");
        }

        var deptResponse = await _organizationClient.GetDepartmentsByCompanyAsync(
            viewModel.Form.CompanyId, cancellationToken);
        viewModel.AvailableParentDepartments = deptResponse.IsSuccess ? deptResponse.Data ?? [] : [];
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var response = await _organizationClient.GetDepartmentByIdAsync(id, cancellationToken);

        if (!response.IsSuccess || response.Data is null)
        {
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Department not found";
            return RedirectToAction(nameof(Index));
        }

        var dept = response.Data;
        var model = new EditDepartmentFormModel
        {
            Id = dept.Id,
            CompanyId = dept.CompanyId,
            Code = dept.Code,
            Name = dept.Name,
            Status = dept.Status
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        EditDepartmentFormModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var response = await _organizationClient.UpdateDepartmentAsync(
            model.Id, model.Name, cancellationToken);

        if (response.IsSuccess)
        {
            TempData["SuccessMessage"] = "Department updated successfully!";
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
            ModelState.AddModelError(string.Empty, response.ErrorMessage ?? "Failed to update department");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
    {
        var response = await _organizationClient.ActivateDepartmentAsync(id, cancellationToken);
        TempData[response.IsSuccess ? "SuccessMessage" : "ErrorMessage"] =
            response.IsSuccess ? "Department activated successfully!" : (response.ErrorMessage ?? "Failed to activate department");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var response = await _organizationClient.DeactivateDepartmentAsync(id, cancellationToken);
        TempData[response.IsSuccess ? "SuccessMessage" : "ErrorMessage"] =
            response.IsSuccess ? "Department deactivated successfully!" : (response.ErrorMessage ?? "Failed to deactivate department");
        return RedirectToAction(nameof(Details), new { id });
    }
}
