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

        // Resolve names for primary references
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

        // Load assignments with resolved names
        var assignmentsResponse = await _personnelClient.GetAssignmentsAsync(emp.Id, cancellationToken: cancellationToken);
        if (assignmentsResponse.IsSuccess && assignmentsResponse.Data != null)
        {
            var displayItems = new List<AssignmentDisplayItem>();
            foreach (var a in assignmentsResponse.Data)
            {
                var item = new AssignmentDisplayItem
                {
                    Id = a.Id,
                    StartDate = a.StartDate,
                    EndDate = a.EndDate,
                    IsPrimary = a.IsPrimary,
                    Status = a.Status
                };

                var c = await _organizationClient.GetCompanyByIdAsync(a.CompanyId, cancellationToken);
                item.CompanyName = c.IsSuccess && c.Data != null ? c.Data.Name : a.CompanyId.ToString();

                var d = await _organizationClient.GetDepartmentByIdAsync(a.DepartmentId, cancellationToken);
                item.DepartmentName = d.IsSuccess && d.Data != null ? d.Data.Name : a.DepartmentId.ToString();

                var p = await _organizationClient.GetPositionByIdAsync(a.PositionId, cancellationToken);
                item.PositionTitle = p.IsSuccess && p.Data != null ? p.Data.Title : a.PositionId.ToString();

                displayItems.Add(item);
            }
            viewModel.Assignments = displayItems;
        }

        // Load direct reports
        var directReportsResponse = await _personnelClient.GetDirectReportsAsync(emp.Id, pageSize: 100, cancellationToken: cancellationToken);
        if (directReportsResponse.IsSuccess && directReportsResponse.Data != null)
            viewModel.DirectReports = directReportsResponse.Data.Items;

        // Load available options for assignment/manager forms
        if (emp.Status != "Terminated")
        {
            var managersResponse = await _personnelClient.GetEmployeesAsync(
                status: "Active", pageSize: 500, cancellationToken: cancellationToken);
            if (managersResponse.IsSuccess && managersResponse.Data != null)
                viewModel.AvailableManagers = managersResponse.Data.Items
                    .Where(e => e.Id != emp.Id).ToList();

            var companiesResponse = await _organizationClient.GetCompaniesAsync(cancellationToken);
            if (companiesResponse.IsSuccess && companiesResponse.Data != null)
                viewModel.AvailableCompanies = companiesResponse.Data
                    .Where(c => c.Status == "Active").ToList();
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
            model.ManagerId,
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

    // ─── Manager ──────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignManager(Guid id, Guid managerId, CancellationToken cancellationToken)
    {
        var response = await _personnelClient.AssignManagerAsync(id, managerId, cancellationToken);

        TempData[response.IsSuccess ? "SuccessMessage" : "ErrorMessage"] =
            response.IsSuccess ? "Manager assigned successfully!" : (response.ErrorMessage ?? "Failed to assign manager");

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveManager(Guid id, CancellationToken cancellationToken)
    {
        var response = await _personnelClient.RemoveManagerAsync(id, cancellationToken);

        TempData[response.IsSuccess ? "SuccessMessage" : "ErrorMessage"] =
            response.IsSuccess ? "Manager removed successfully!" : (response.ErrorMessage ?? "Failed to remove manager");

        return RedirectToAction(nameof(Details), new { id });
    }

    // ─── Assignments ──────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddAssignment(
        Guid id, Guid companyId, Guid departmentId, Guid positionId,
        DateOnly startDate, bool isPrimary,
        CancellationToken cancellationToken)
    {
        var response = await _personnelClient.AddAssignmentAsync(
            id, companyId, departmentId, positionId, startDate, isPrimary, cancellationToken);

        TempData[response.IsSuccess ? "SuccessMessage" : "ErrorMessage"] =
            response.IsSuccess ? "Assignment added successfully!" : (response.ErrorMessage ?? "Failed to add assignment");

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EndAssignment(Guid id, Guid assignmentId, CancellationToken cancellationToken)
    {
        var endDate = DateOnly.FromDateTime(DateTime.Today);
        var response = await _personnelClient.EndAssignmentAsync(id, assignmentId, endDate, cancellationToken);

        TempData[response.IsSuccess ? "SuccessMessage" : "ErrorMessage"] =
            response.IsSuccess ? "Assignment ended successfully!" : (response.ErrorMessage ?? "Failed to end assignment");

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetPrimaryAssignment(Guid id, Guid assignmentId, CancellationToken cancellationToken)
    {
        var response = await _personnelClient.SetPrimaryAssignmentAsync(id, assignmentId, cancellationToken);

        TempData[response.IsSuccess ? "SuccessMessage" : "ErrorMessage"] =
            response.IsSuccess ? "Primary assignment updated!" : (response.ErrorMessage ?? "Failed to set primary assignment");

        return RedirectToAction(nameof(Details), new { id });
    }

    // ─── AJAX: Load departments/positions by company ──────────────────────

    [HttpGet]
    public async Task<IActionResult> GetDepartmentsByCompany(Guid companyId, CancellationToken cancellationToken)
    {
        var response = await _organizationClient.GetDepartmentsByCompanyAsync(companyId, cancellationToken);
        if (response.IsSuccess && response.Data != null)
            return Json(response.Data.Where(d => d.Status == "Active").Select(d => new { d.Id, d.Name }));
        return Json(Array.Empty<object>());
    }

    [HttpGet]
    public async Task<IActionResult> GetPositionsByCompany(Guid companyId, CancellationToken cancellationToken)
    {
        var response = await _organizationClient.GetPositionsByCompanyAsync(companyId, cancellationToken);
        if (response.IsSuccess && response.Data != null)
            return Json(response.Data.Where(p => p.Status == "Active").Select(p => new { p.Id, p.Title }));
        return Json(Array.Empty<object>());
    }
}
