using HRM.Web.Models;
using HRM.Web.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Web.Controllers;

[Authorize]
public class LeaveController : Controller
{
    private readonly IAttendanceApiClient _attendanceClient;
    private readonly ILogger<LeaveController> _logger;

    public LeaveController(
        IAttendanceApiClient attendanceClient,
        ILogger<LeaveController> logger)
    {
        _attendanceClient = attendanceClient;
        _logger = logger;
    }

    // ─── Leave Types ─────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Types(
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var response = await _attendanceClient.GetLeaveTypesAsync(isActive, cancellationToken);

        var viewModel = new LeaveTypeListViewModel { IsActive = isActive };

        if (response.IsSuccess && response.Data != null)
        {
            viewModel.LeaveTypes = response.Data;
        }
        else
        {
            _logger.LogError("Failed to get leave types: {ErrorMessage}", response.ErrorMessage);
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to load leave types";
        }

        return View(viewModel);
    }

    [HttpGet]
    public IActionResult CreateType() => View(new CreateLeaveTypeFormModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateType(
        CreateLeaveTypeFormModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View(model);

        var response = await _attendanceClient.CreateLeaveTypeAsync(
            model.Name, model.DefaultDaysPerYear, model.IsPaid, model.Description, cancellationToken);

        if (response.IsSuccess)
        {
            TempData["SuccessMessage"] = "Leave type created successfully!";
            return RedirectToAction(nameof(Types));
        }

        if (response.ValidationErrors != null)
        {
            foreach (var (field, errors) in response.ValidationErrors)
                foreach (var error in errors)
                    ModelState.AddModelError(field, error);
        }
        else
        {
            ModelState.AddModelError(string.Empty, response.ErrorMessage ?? "Failed to create leave type");
        }

        return View(model);
    }

    // ─── Leave Requests ──────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Requests(
        Guid? employeeId = null,
        string? status = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var response = await _attendanceClient.GetLeaveRequestsAsync(
            employeeId, status, pageNumber, pageSize, cancellationToken);

        var viewModel = new LeaveRequestListViewModel
        {
            EmployeeId = employeeId,
            Status = status,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        if (response.IsSuccess && response.Data != null)
        {
            viewModel.Requests = response.Data;
        }
        else
        {
            _logger.LogError("Failed to get leave requests: {ErrorMessage}", response.ErrorMessage);
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to load leave requests";
        }

        return View(viewModel);
    }

    // ─── Submit Leave Request ────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> Submit(CancellationToken cancellationToken)
    {
        var viewModel = new SubmitLeaveRequestViewModel();
        await LoadLeaveTypesAsync(viewModel, cancellationToken);
        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(
        SubmitLeaveRequestViewModel viewModel,
        CancellationToken cancellationToken)
    {
        var model = viewModel.Form;
        if (!ModelState.IsValid)
        {
            await LoadLeaveTypesAsync(viewModel, cancellationToken);
            return View(viewModel);
        }

        var response = await _attendanceClient.SubmitLeaveRequestAsync(
            model.LeaveTypeId, model.StartDate, model.EndDate, model.Reason, cancellationToken);

        if (response.IsSuccess)
        {
            TempData["SuccessMessage"] = "Leave request submitted successfully!";
            return RedirectToAction(nameof(Requests));
        }

        if (response.ValidationErrors != null)
        {
            foreach (var (field, errors) in response.ValidationErrors)
                foreach (var error in errors)
                    ModelState.AddModelError(field, error);
        }
        else
        {
            ModelState.AddModelError(string.Empty, response.ErrorMessage ?? "Failed to submit leave request");
        }

        await LoadLeaveTypesAsync(viewModel, cancellationToken);
        return View(viewModel);
    }

    // ─── Approve / Reject ────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(
        ApproveLeaveRequestFormModel model,
        CancellationToken cancellationToken)
    {
        var response = await _attendanceClient.ApproveLeaveRequestAsync(
            model.LeaveRequestId, model.IsApproved, model.Notes, cancellationToken);

        if (response.IsSuccess)
            TempData["SuccessMessage"] = model.IsApproved ? "Leave request approved!" : "Leave request rejected!";
        else
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to process leave request";

        return RedirectToAction(nameof(Requests));
    }

    // ─── Cancel ──────────────────────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        var response = await _attendanceClient.CancelLeaveRequestAsync(id, cancellationToken);

        if (response.IsSuccess)
            TempData["SuccessMessage"] = "Leave request cancelled!";
        else
            TempData["ErrorMessage"] = response.ErrorMessage ?? "Failed to cancel leave request";

        return RedirectToAction(nameof(Requests));
    }

    private async Task LoadLeaveTypesAsync(
        SubmitLeaveRequestViewModel viewModel,
        CancellationToken cancellationToken)
    {
        var response = await _attendanceClient.GetLeaveTypesAsync(isActive: true, cancellationToken: cancellationToken);
        if (response.IsSuccess && response.Data != null)
            viewModel.AvailableLeaveTypes = response.Data;
    }
}
