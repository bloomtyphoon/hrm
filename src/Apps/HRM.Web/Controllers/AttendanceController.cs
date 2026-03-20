using HRM.Web.Models;
using HRM.Web.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Web.Controllers;

[Authorize]
public class AttendanceController : Controller
{
    private readonly IAttendanceApiClient _attendanceClient;
    private readonly IPersonnelApiClient _personnelClient;
    private readonly ICompanyContext _companyContext;
    private readonly ILogger<AttendanceController> _logger;

    public AttendanceController(
        IAttendanceApiClient attendanceClient,
        IPersonnelApiClient personnelClient,
        ICompanyContext companyContext,
        ILogger<AttendanceController> logger)
    {
        _attendanceClient = attendanceClient;
        _personnelClient = personnelClient;
        _companyContext = companyContext;
        _logger = logger;
    }

    // ─── My Attendance (current user) ───────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> MyAttendance(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var response = await _attendanceClient.GetMyAttendanceAsync(
            fromDate, toDate, pageNumber, pageSize, cancellationToken);

        var viewModel = new MyAttendanceViewModel
        {
            FromDate = fromDate,
            ToDate = toDate,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        if (response.IsSuccess && response.Data != null)
        {
            viewModel.Records = response.Data;
        }
        else
        {
            _logger.LogError("Failed to get attendance: {ErrorMessage}", response.ErrorMessage);
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to load attendance records";
            viewModel.Records = new PagedResult<AttendanceSummaryResponse>();
        }

        return View(viewModel);
    }

    // ─── Check In ────────────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult CheckIn()
    {
        return View(new CheckInFormModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn(
        CheckInFormModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        var response = await _attendanceClient.CheckInAsync(
            model.CheckInTimeUtc, model.Notes, cancellationToken);

        if (response.IsSuccess)
        {
            TempData["SuccessMessage"] = "Checked in successfully!";
            return RedirectToAction(nameof(MyAttendance));
        }

        if (response.ValidationErrors != null)
        {
            foreach (var (field, errors) in response.ValidationErrors)
                foreach (var error in errors)
                    ModelState.AddModelError(field, error);
        }
        else
        {
            ModelState.AddModelError(string.Empty, response.ErrorMessage ?? "Failed to check in");
        }

        return View(model);
    }

    // ─── Check Out ───────────────────────────────────────────────────────────

    [HttpGet]
    public IActionResult CheckOut()
    {
        return View(new CheckOutFormModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckOut(
        CheckOutFormModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        var response = await _attendanceClient.CheckOutAsync(
            model.CheckOutTimeUtc, model.Notes, cancellationToken);

        if (response.IsSuccess)
        {
            TempData["SuccessMessage"] = "Checked out successfully!";
            return RedirectToAction(nameof(MyAttendance));
        }

        if (response.ValidationErrors != null)
        {
            foreach (var (field, errors) in response.ValidationErrors)
                foreach (var error in errors)
                    ModelState.AddModelError(field, error);
        }
        else
        {
            ModelState.AddModelError(string.Empty, response.ErrorMessage ?? "Failed to check out");
        }

        return View(model);
    }

    // ─── Team Attendance ─────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> TeamAttendance(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var response = await _attendanceClient.GetTeamAttendanceAsync(
            fromDate, toDate, pageNumber, pageSize, cancellationToken);

        var viewModel = new TeamAttendanceViewModel
        {
            FromDate = fromDate,
            ToDate = toDate,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        if (response.IsSuccess && response.Data != null)
        {
            viewModel.Records = response.Data;
        }
        else
        {
            _logger.LogError("Failed to get team attendance: {ErrorMessage}", response.ErrorMessage);
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to load team attendance records";
            viewModel.Records = new PagedResult<AttendanceSummaryResponse>();
        }

        return View(viewModel);
    }

    // ─── Attendance Summary / Dashboard ──────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Summary(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var response = await _attendanceClient.GetAttendanceSummaryAsync(
            fromDate, toDate, cancellationToken);

        var viewModel = new AttendanceSummaryViewModel
        {
            FromDate = fromDate,
            ToDate = toDate
        };

        if (response.IsSuccess && response.Data != null)
        {
            viewModel.DailySummaries = response.Data;
        }
        else
        {
            _logger.LogError("Failed to get attendance summary: {ErrorMessage}", response.ErrorMessage);
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to load attendance summary";
        }

        return View(viewModel);
    }

    // ─── Employee Attendance (HR/Manager view) ───────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Index(
        Guid employeeId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var viewModel = new AttendanceListViewModel
        {
            EmployeeId = employeeId,
            FromDate = fromDate,
            ToDate = toDate,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        // Try to get employee name for display
        var employeeResponse = await _personnelClient.GetEmployeeByIdAsync(employeeId, cancellationToken);
        if (employeeResponse.IsSuccess && employeeResponse.Data != null)
        {
            viewModel.EmployeeName = employeeResponse.Data.FullName;
        }

        var response = await _attendanceClient.GetEmployeeAttendanceAsync(
            employeeId, fromDate, toDate, pageNumber, pageSize, cancellationToken);

        if (response.IsSuccess && response.Data != null)
        {
            viewModel.Records = response.Data;
        }
        else
        {
            _logger.LogError("Failed to get employee attendance: {ErrorMessage}", response.ErrorMessage);
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to load attendance records";
            viewModel.Records = new PagedResult<AttendanceSummaryResponse>();
        }

        return View(viewModel);
    }

    // ─── Attendance Record Detail ─────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var response = await _attendanceClient.GetAttendanceByIdAsync(id, cancellationToken);

        if (!response.IsSuccess || response.Data is null)
        {
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Attendance record not found";
            return RedirectToAction(nameof(MyAttendance));
        }

        var viewModel = new AttendanceDetailViewModel { Record = response.Data };

        var empResponse = await _personnelClient.GetEmployeeByIdAsync(response.Data.EmployeeId, cancellationToken);
        if (empResponse.IsSuccess && empResponse.Data != null)
            viewModel.EmployeeName = empResponse.Data.FullName;

        return View(viewModel);
    }

    // ─── Edit Attendance Record (HR) ─────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var response = await _attendanceClient.GetAttendanceByIdAsync(id, cancellationToken);

        if (!response.IsSuccess || response.Data is null)
        {
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Attendance record not found";
            return RedirectToAction(nameof(MyAttendance));
        }

        var viewModel = new EditAttendanceViewModel
        {
            Form = new EditAttendanceFormModel
            {
                RecordId = response.Data.Id,
                EmployeeId = response.Data.EmployeeId,
                CheckInTimeUtc = response.Data.CheckInTimeUtc,
                CheckOutTimeUtc = response.Data.CheckOutTimeUtc,
                Notes = response.Data.Notes
            }
        };

        var empResponse = await _personnelClient.GetEmployeeByIdAsync(response.Data.EmployeeId, cancellationToken);
        if (empResponse.IsSuccess && empResponse.Data != null)
            viewModel.EmployeeName = empResponse.Data.FullName;

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        EditAttendanceViewModel viewModel,
        CancellationToken cancellationToken)
    {
        var model = viewModel.Form;
        if (!ModelState.IsValid)
            return View(viewModel);

        var response = await _attendanceClient.UpdateAttendanceAsync(
            model.RecordId,
            model.CheckInTimeUtc,
            model.CheckOutTimeUtc,
            model.Notes,
            cancellationToken);

        if (response.IsSuccess)
        {
            TempData["SuccessMessage"] = "Attendance record updated successfully!";
            return RedirectToAction(nameof(Details), new { id = model.RecordId });
        }

        if (response.ValidationErrors != null)
        {
            foreach (var (field, errors) in response.ValidationErrors)
                foreach (var error in errors)
                    ModelState.AddModelError(field, error);
        }
        else
        {
            ModelState.AddModelError(string.Empty, response.ErrorMessage ?? "Failed to update attendance record");
        }

        return View(viewModel);
    }

    // ─── Delete Attendance Record (HR) ───────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, Guid? employeeId, CancellationToken cancellationToken)
    {
        var response = await _attendanceClient.DeleteAttendanceAsync(id, cancellationToken);

        if (response.IsSuccess)
        {
            TempData["SuccessMessage"] = "Attendance record deleted successfully!";
        }
        else
        {
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to delete attendance record";
        }

        if (employeeId.HasValue)
            return RedirectToAction(nameof(Index), new { employeeId = employeeId.Value });

        return RedirectToAction(nameof(MyAttendance));
    }

    // ─── HR Manual Record ─────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> RecordManual(CancellationToken cancellationToken)
    {
        var viewModel = new RecordManualAttendanceViewModel();
        await LoadAvailableEmployeesAsync(viewModel, cancellationToken);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RecordManual(
        RecordManualAttendanceViewModel viewModel,
        CancellationToken cancellationToken)
    {
        var model = viewModel.Form;
        if (!ModelState.IsValid)
        {
            await LoadAvailableEmployeesAsync(viewModel, cancellationToken);
            return View(viewModel);
        }

        var companyId = _companyContext.IsAllCompanies ? null : _companyContext.SelectedCompanyId;

        var response = await _attendanceClient.RecordManualAttendanceAsync(
            model.EmployeeId,
            model.Date,
            model.CheckInTimeUtc,
            model.CheckOutTimeUtc,
            companyId,
            model.Notes,
            cancellationToken);

        if (response.IsSuccess)
        {
            TempData["SuccessMessage"] = "Attendance recorded successfully!";
            return RedirectToAction(nameof(Index), new { employeeId = model.EmployeeId });
        }

        if (response.ValidationErrors != null)
        {
            foreach (var (field, errors) in response.ValidationErrors)
                foreach (var error in errors)
                    ModelState.AddModelError(field, error);
        }
        else
        {
            ModelState.AddModelError(string.Empty, response.ErrorMessage ?? "Failed to record attendance");
        }

        await LoadAvailableEmployeesAsync(viewModel, cancellationToken);
        return View(viewModel);
    }

    private async Task LoadAvailableEmployeesAsync(
        RecordManualAttendanceViewModel viewModel,
        CancellationToken cancellationToken)
    {
        var companyId = _companyContext.IsAllCompanies ? null : _companyContext.SelectedCompanyId;
        var employeesResponse = await _personnelClient.GetEmployeesAsync(
            status: "Active", companyId: companyId,
            pageSize: 500, cancellationToken: cancellationToken);
        if (employeesResponse.IsSuccess && employeesResponse.Data != null)
        {
            viewModel.AvailableEmployees = employeesResponse.Data.Items;
        }
    }
}
