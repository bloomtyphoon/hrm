using HRM.Web.Models;
using HRM.Web.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Web.Controllers;

[Authorize]
public class EmployeeController : Controller
{
    private readonly IPersonnelApiClient _personnelClient;
    private readonly IOrganizationApiClient _organizationClient;
    private readonly ICompanyContext _companyContext;
    private readonly ILogger<EmployeeController> _logger;

    public EmployeeController(
        IPersonnelApiClient personnelClient,
        IOrganizationApiClient organizationClient,
        ICompanyContext companyContext,
        ILogger<EmployeeController> logger)
    {
        _personnelClient = personnelClient;
        _organizationClient = organizationClient;
        _companyContext = companyContext;
        _logger = logger;
    }

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

        var companyId = _companyContext.IsAllCompanies ? null : _companyContext.SelectedCompanyId;

        var response = await _personnelClient.GetEmployeesAsync(
            searchTerm, status, companyId,
            departmentId: null,
            pageNumber, pageSize, cancellationToken);

        var viewModel = new EmployeeListViewModel
        {
            SearchTerm = searchTerm,
            StatusFilter = status,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        if (response.IsSuccess && response.Data != null)
        {
            viewModel.Employees = response.Data;
        }
        else
        {
            _logger.LogError("Failed to get employees: {ErrorMessage}", response.ErrorMessage);
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to load employees";
            viewModel.Employees = new PagedResult<EmployeeSummaryResponse>();
        }

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var response = await _personnelClient.GetEmployeeByIdAsync(id, cancellationToken);

        if (!response.IsSuccess || response.Data is null)
        {
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Employee not found";
            return RedirectToAction(nameof(Index));
        }

        var emp = response.Data;
        var viewModel = new EmployeeDetailViewModel { Employee = emp };

        if (emp.ManagerId.HasValue)
        {
            var mgr = await _personnelClient.GetEmployeeByIdAsync(emp.ManagerId.Value, cancellationToken);
            if (mgr.IsSuccess && mgr.Data != null)
                viewModel.ManagerName = mgr.Data.FullName;
        }

        if (emp.PrimaryCompanyId.HasValue)
        {
            var company = await _organizationClient.GetCompanyByIdAsync(emp.PrimaryCompanyId.Value, cancellationToken);
            if (company.IsSuccess && company.Data != null)
                viewModel.PrimaryCompanyName = company.Data.Name;
        }

        if (emp.PrimaryDepartmentId.HasValue)
        {
            var dept = await _organizationClient.GetDepartmentByIdAsync(emp.PrimaryDepartmentId.Value, cancellationToken);
            if (dept.IsSuccess && dept.Data != null)
                viewModel.PrimaryDepartmentName = dept.Data.Name;
        }

        if (emp.PrimaryPositionId.HasValue)
        {
            var pos = await _organizationClient.GetPositionByIdAsync(emp.PrimaryPositionId.Value, cancellationToken);
            if (pos.IsSuccess && pos.Data != null)
                viewModel.PrimaryPositionTitle = pos.Data.Title;
        }

        return View(viewModel);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateEmployeeFormModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateEmployeeFormModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var response = await _personnelClient.CreateEmployeeAsync(
            model.EmployeeCode,
            model.FirstName,
            model.LastName,
            model.Email,
            model.HireDate,
            model.Phone,
            model.DateOfBirth,
            cancellationToken);

        if (response.IsSuccess && response.Data != null)
        {
            TempData["SuccessMessage"] = $"Employee '{response.Data.FullName}' created successfully!";
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
            ModelState.AddModelError(string.Empty, response.ErrorMessage ?? "Failed to create employee");
        }

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var response = await _personnelClient.GetEmployeeByIdAsync(id, cancellationToken);

        if (!response.IsSuccess || response.Data is null)
        {
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Employee not found";
            return RedirectToAction(nameof(Index));
        }

        var emp = response.Data;
        var model = new EditEmployeeFormModel
        {
            Id = emp.Id,
            EmployeeCode = emp.EmployeeCode,
            FirstName = emp.FirstName,
            LastName = emp.LastName,
            Email = emp.Email,
            Phone = emp.Phone,
            HireDate = emp.HireDate,
            DateOfBirth = emp.DateOfBirth,
            Status = emp.Status
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        EditEmployeeFormModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var response = await _personnelClient.UpdateEmployeeAsync(
            model.Id,
            model.FirstName,
            model.LastName,
            model.Email,
            model.Phone,
            model.DateOfBirth,
            cancellationToken);

        if (response.IsSuccess)
        {
            TempData["SuccessMessage"] = "Employee updated successfully!";
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
            ModelState.AddModelError(string.Empty, response.ErrorMessage ?? "Failed to update employee");
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Terminate(Guid id, CancellationToken cancellationToken)
    {
        var terminationDate = DateOnly.FromDateTime(DateTime.Today);
        var response = await _personnelClient.TerminateEmployeeAsync(id, terminationDate, cancellationToken);

        TempData[response.IsSuccess ? "SuccessMessage" : "ErrorMessage"] =
            response.IsSuccess ? "Employee terminated successfully." : (response.ErrorMessage ?? "Failed to terminate employee");

        return RedirectToAction(nameof(Details), new { id });
    }
}
