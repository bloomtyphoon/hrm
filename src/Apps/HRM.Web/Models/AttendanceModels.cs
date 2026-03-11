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
