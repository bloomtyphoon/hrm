using System.ComponentModel.DataAnnotations;

namespace HRM.Web.Models;

// ─── Response Models ────────────────────────────────────────────────────────

/// <summary>Summary data for attendance list views.</summary>
public sealed class AttendanceSummaryResponse
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public DateOnly Date { get; set; }
    public DateTime CheckInTimeUtc { get; set; }
    public DateTime? CheckOutTimeUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsManualEntry { get; set; }
}

/// <summary>View model for attendance details page with resolved names.</summary>
public sealed class AttendanceDetailViewModel
{
    public AttendanceDetailResponse Record { get; set; } = new();
    public string? EmployeeName { get; set; }
}

/// <summary>Full detail data for a single attendance record.</summary>
public sealed class AttendanceDetailResponse
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public DateOnly Date { get; set; }
    public DateTime CheckInTimeUtc { get; set; }
    public DateTime? CheckOutTimeUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsManualEntry { get; set; }
    public string? Notes { get; set; }
    public Guid? CompanyId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
}

// ─── View Models ─────────────────────────────────────────────────────────────

/// <summary>View model for HR's employee attendance list page.</summary>
public sealed class AttendanceListViewModel
{
    public PagedResult<AttendanceSummaryResponse> Records { get; set; } = new();
    public Guid EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>View model for the current user's own attendance page.</summary>
public sealed class MyAttendanceViewModel
{
    public PagedResult<AttendanceSummaryResponse> Records { get; set; } = new();
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

// ─── Form Models ─────────────────────────────────────────────────────────────

/// <summary>Form model for employee self check-in.</summary>
public sealed class CheckInFormModel
{
    [Display(Name = "Check-In Time (UTC)")]
    public DateTime? CheckInTimeUtc { get; set; }

    [StringLength(500)]
    [Display(Name = "Notes")]
    public string? Notes { get; set; }
}

/// <summary>Form model for employee self check-out.</summary>
public sealed class CheckOutFormModel
{
    [Display(Name = "Check-Out Time (UTC)")]
    public DateTime? CheckOutTimeUtc { get; set; }

    [StringLength(500)]
    [Display(Name = "Notes")]
    public string? Notes { get; set; }
}

/// <summary>View model for the HR manual attendance entry page.</summary>
public sealed class RecordManualAttendanceViewModel
{
    public RecordManualAttendanceFormModel Form { get; set; } = new();
    public IReadOnlyList<EmployeeSummaryResponse> AvailableEmployees { get; set; } = [];
}

/// <summary>Form model for HR manual attendance entry.</summary>
public sealed class RecordManualAttendanceFormModel
{
    [Required(ErrorMessage = "Employee ID is required")]
    [Display(Name = "Employee ID")]
    public Guid EmployeeId { get; set; }

    [Required(ErrorMessage = "Date is required")]
    [Display(Name = "Date")]
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required(ErrorMessage = "Check-in time is required")]
    [Display(Name = "Check-In Time (UTC)")]
    public DateTime CheckInTimeUtc { get; set; } = DateTime.UtcNow.Date.AddHours(8);

    [Required(ErrorMessage = "Check-out time is required")]
    [Display(Name = "Check-Out Time (UTC)")]
    public DateTime CheckOutTimeUtc { get; set; } = DateTime.UtcNow.Date.AddHours(17);

    [Display(Name = "Company ID")]
    public Guid? CompanyId { get; set; }

    [StringLength(500)]
    [Display(Name = "Notes")]
    public string? Notes { get; set; }
}

/// <summary>View model for HR editing an attendance record.</summary>
public sealed class EditAttendanceViewModel
{
    public EditAttendanceFormModel Form { get; set; } = new();
    public string? EmployeeName { get; set; }
}

/// <summary>Form model for HR editing an attendance record.</summary>
public sealed class EditAttendanceFormModel
{
    public Guid RecordId { get; set; }

    public Guid EmployeeId { get; set; }

    [Required(ErrorMessage = "Check-in time is required")]
    [Display(Name = "Check-In Time (UTC)")]
    public DateTime CheckInTimeUtc { get; set; }

    [Display(Name = "Check-Out Time (UTC)")]
    public DateTime? CheckOutTimeUtc { get; set; }

    [StringLength(500)]
    [Display(Name = "Notes")]
    public string? Notes { get; set; }
}

/// <summary>Response model for daily attendance summary.</summary>
public sealed class AttendanceDailySummaryResponse
{
    public DateOnly Date { get; set; }
    public int TotalEmployees { get; set; }
    public int CheckedIn { get; set; }
    public int CheckedOut { get; set; }
    public int ManualEntries { get; set; }
}

/// <summary>View model for the team attendance page.</summary>
public sealed class TeamAttendanceViewModel
{
    public PagedResult<AttendanceSummaryResponse> Records { get; set; } = new();
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>View model for the attendance summary/dashboard page.</summary>
public sealed class AttendanceSummaryViewModel
{
    public IReadOnlyList<AttendanceDailySummaryResponse> DailySummaries { get; set; } = [];
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}

// ─── Shift Response Models ──────────────────────────────────────────────────

public sealed class ShiftResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public Guid? CompanyId { get; set; }
}

public sealed class ShiftAssignmentResponse
{
    public Guid Id { get; set; }
    public Guid ShiftId { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public Guid EmployeeId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
}

// ─── Shift View Models ──────────────────────────────────────────────────────

public sealed class ShiftListViewModel
{
    public PagedResult<ShiftResponse> Shifts { get; set; } = new();
    public bool? IsActive { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class CreateShiftFormModel
{
    [Required(ErrorMessage = "Shift name is required")]
    [StringLength(200)]
    [Display(Name = "Shift Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Start time is required")]
    [Display(Name = "Start Time")]
    public TimeOnly StartTime { get; set; } = new(8, 0);

    [Required(ErrorMessage = "End time is required")]
    [Display(Name = "End Time")]
    public TimeOnly EndTime { get; set; } = new(17, 0);

    [StringLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Company ID")]
    public Guid? CompanyId { get; set; }
}

public sealed class ShiftAssignmentListViewModel
{
    public PagedResult<ShiftAssignmentResponse> Assignments { get; set; } = new();
    public Guid? EmployeeId { get; set; }
    public Guid? ShiftId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class AssignShiftFormModel
{
    [Required(ErrorMessage = "Shift is required")]
    [Display(Name = "Shift")]
    public Guid ShiftId { get; set; }

    [Required(ErrorMessage = "Employee is required")]
    [Display(Name = "Employee")]
    public Guid EmployeeId { get; set; }

    [Required(ErrorMessage = "Effective from date is required")]
    [Display(Name = "Effective From")]
    public DateOnly EffectiveFrom { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Display(Name = "Effective To")]
    public DateOnly? EffectiveTo { get; set; }

    [Display(Name = "Company ID")]
    public Guid? CompanyId { get; set; }
}

public sealed class AssignShiftViewModel
{
    public AssignShiftFormModel Form { get; set; } = new();
    public IReadOnlyList<ShiftResponse> AvailableShifts { get; set; } = [];
    public IReadOnlyList<EmployeeSummaryResponse> AvailableEmployees { get; set; } = [];
}

// ─── Leave Response Models ──────────────────────────────────────────────────

public sealed class LeaveTypeResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DefaultDaysPerYear { get; set; }
    public bool IsPaid { get; set; }
    public bool IsActive { get; set; }
}

public sealed class LeaveRequestResponse
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid LeaveTypeId { get; set; }
    public string LeaveTypeName { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int TotalDays { get; set; }
    public string? Reason { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? ApprovedByEmployeeId { get; set; }
    public DateTime? DecisionDateUtc { get; set; }
    public string? DecisionNotes { get; set; }
}

// ─── Leave View Models ──────────────────────────────────────────────────────

public sealed class LeaveTypeListViewModel
{
    public IReadOnlyList<LeaveTypeResponse> LeaveTypes { get; set; } = [];
    public bool? IsActive { get; set; }
}

public sealed class CreateLeaveTypeFormModel
{
    [Required(ErrorMessage = "Leave type name is required")]
    [StringLength(200)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Default days per year is required")]
    [Range(1, 365)]
    [Display(Name = "Default Days Per Year")]
    public int DefaultDaysPerYear { get; set; } = 12;

    [Display(Name = "Is Paid")]
    public bool IsPaid { get; set; } = true;

    [StringLength(500)]
    [Display(Name = "Description")]
    public string? Description { get; set; }
}

public sealed class LeaveRequestListViewModel
{
    public PagedResult<LeaveRequestResponse> Requests { get; set; } = new();
    public Guid? EmployeeId { get; set; }
    public string? Status { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class SubmitLeaveRequestFormModel
{
    [Required(ErrorMessage = "Leave type is required")]
    [Display(Name = "Leave Type")]
    public Guid LeaveTypeId { get; set; }

    [Required(ErrorMessage = "Start date is required")]
    [Display(Name = "Start Date")]
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required(ErrorMessage = "End date is required")]
    [Display(Name = "End Date")]
    public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [StringLength(1000)]
    [Display(Name = "Reason")]
    public string? Reason { get; set; }
}

public sealed class SubmitLeaveRequestViewModel
{
    public SubmitLeaveRequestFormModel Form { get; set; } = new();
    public IReadOnlyList<LeaveTypeResponse> AvailableLeaveTypes { get; set; } = [];
}

public sealed class ApproveLeaveRequestFormModel
{
    public Guid LeaveRequestId { get; set; }
    public bool IsApproved { get; set; }

    [StringLength(1000)]
    [Display(Name = "Notes")]
    public string? Notes { get; set; }
}

// ─── Leave Approval Settings Models ────────────────────────────────────────

public sealed class LeaveApprovalSettingsResponse
{
    public bool RequiresApproval { get; set; }
    public int MaxApprovalLevels { get; set; }
    public int? AutoApproveIfDaysLessThanOrEqual { get; set; }
    public bool AllowSelfCancel { get; set; }
    public bool NotifyOnDecision { get; set; }
}

public sealed class LeaveApprovalSettingsFormModel
{
    [Display(Name = "Requires Approval")]
    public bool RequiresApproval { get; set; } = true;

    [Required(ErrorMessage = "Max approval levels is required")]
    [Range(1, 5, ErrorMessage = "Must be between 1 and 5")]
    [Display(Name = "Max Approval Levels")]
    public int MaxApprovalLevels { get; set; } = 1;

    [Range(0, 365)]
    [Display(Name = "Auto-approve if days <=")]
    public int? AutoApproveIfDaysLessThanOrEqual { get; set; }

    [Display(Name = "Allow Self-Cancel")]
    public bool AllowSelfCancel { get; set; } = true;

    [Display(Name = "Notify on Decision")]
    public bool NotifyOnDecision { get; set; } = true;
}
