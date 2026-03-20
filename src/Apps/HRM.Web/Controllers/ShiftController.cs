using HRM.Web.Models;
using HRM.Web.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Web.Controllers;

[Authorize]
public class ShiftController : Controller
{
    private readonly IAttendanceApiClient _attendanceClient;
    private readonly IPersonnelApiClient _personnelClient;
    private readonly ICompanyContext _companyContext;
    private readonly ILogger<ShiftController> _logger;

    public ShiftController(
        IAttendanceApiClient attendanceClient,
        IPersonnelApiClient personnelClient,
        ICompanyContext companyContext,
        ILogger<ShiftController> logger)
    {
        _attendanceClient = attendanceClient;
        _personnelClient = personnelClient;
        _companyContext = companyContext;
        _logger = logger;
    }

    // ─── Shift List ──────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Index(
        bool? isActive = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var response = await _attendanceClient.GetShiftsAsync(isActive, pageNumber, pageSize, cancellationToken);

        var viewModel = new ShiftListViewModel
        {
            IsActive = isActive,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        if (response.IsSuccess && response.Data != null)
        {
            viewModel.Shifts = response.Data;
        }
        else
        {
            _logger.LogError("Failed to get shifts: {ErrorMessage}", response.ErrorMessage);
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to load shifts";
        }

        return View(viewModel);
    }

    // ─── Create Shift ────────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult Create() => View(new CreateShiftFormModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateShiftFormModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        var companyId = model.CompanyId ?? (_companyContext.IsAllCompanies ? null : _companyContext.SelectedCompanyId);

        var response = await _attendanceClient.CreateShiftAsync(
            model.Name, model.StartTime, model.EndTime, companyId, model.Description, cancellationToken);

        if (response.IsSuccess)
        {
            TempData["SuccessMessage"] = "Shift created successfully!";
            return RedirectToAction(nameof(Index));
        }

        if (response.ValidationErrors != null)
        {
            foreach (var (field, errors) in response.ValidationErrors)
                foreach (var error in errors)
                    ModelState.AddModelError(field, error);
        }
        else
        {
            ModelState.AddModelError(string.Empty, response.ErrorMessage ?? "Failed to create shift");
        }

        return View(model);
    }

    // ─── Delete Shift ────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var response = await _attendanceClient.DeleteShiftAsync(id, cancellationToken);

        if (response.IsSuccess)
            TempData["SuccessMessage"] = "Shift deleted successfully!";
        else
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to delete shift";

        return RedirectToAction(nameof(Index));
    }

    // ─── Shift Assignments ───────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Assignments(
        Guid? employeeId = null,
        Guid? shiftId = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var response = await _attendanceClient.GetShiftAssignmentsAsync(
            employeeId, shiftId, pageNumber, pageSize, cancellationToken);

        var viewModel = new ShiftAssignmentListViewModel
        {
            EmployeeId = employeeId,
            ShiftId = shiftId,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        if (response.IsSuccess && response.Data != null)
        {
            viewModel.Assignments = response.Data;
        }
        else
        {
            _logger.LogError("Failed to get shift assignments: {ErrorMessage}", response.ErrorMessage);
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to load shift assignments";
        }

        return View(viewModel);
    }

    // ─── Assign Shift ────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Assign(CancellationToken cancellationToken)
    {
        var viewModel = new AssignShiftViewModel();
        await LoadAssignShiftDataAsync(viewModel, cancellationToken);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(
        AssignShiftViewModel viewModel,
        CancellationToken cancellationToken)
    {
        var model = viewModel.Form;
        if (!ModelState.IsValid)
        {
            await LoadAssignShiftDataAsync(viewModel, cancellationToken);
            return View(viewModel);
        }

        var companyId = model.CompanyId ?? (_companyContext.IsAllCompanies ? null : _companyContext.SelectedCompanyId);

        var response = await _attendanceClient.AssignShiftAsync(
            model.ShiftId, model.EmployeeId, model.EffectiveFrom,
            model.EffectiveTo, companyId, cancellationToken);

        if (response.IsSuccess)
        {
            TempData["SuccessMessage"] = "Shift assigned successfully!";
            return RedirectToAction(nameof(Assignments));
        }

        if (response.ValidationErrors != null)
        {
            foreach (var (field, errors) in response.ValidationErrors)
                foreach (var error in errors)
                    ModelState.AddModelError(field, error);
        }
        else
        {
            ModelState.AddModelError(string.Empty, response.ErrorMessage ?? "Failed to assign shift");
        }

        await LoadAssignShiftDataAsync(viewModel, cancellationToken);
        return View(viewModel);
    }

    private async Task LoadAssignShiftDataAsync(
        AssignShiftViewModel viewModel,
        CancellationToken cancellationToken)
    {
        var shiftsResponse = await _attendanceClient.GetShiftsAsync(
            isActive: true, pageSize: 500, cancellationToken: cancellationToken);
        if (shiftsResponse.IsSuccess && shiftsResponse.Data != null)
            viewModel.AvailableShifts = shiftsResponse.Data.Items;

        var companyId = _companyContext.IsAllCompanies ? null : _companyContext.SelectedCompanyId;
        var employeesResponse = await _personnelClient.GetEmployeesAsync(
            status: "Active", companyId: companyId,
            pageSize: 500, cancellationToken: cancellationToken);
        if (employeesResponse.IsSuccess && employeesResponse.Data != null)
            viewModel.AvailableEmployees = employeesResponse.Data.Items;
    }
}
