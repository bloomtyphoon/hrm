namespace HRM.Modules.Attendance.Api.Contracts;

/// <summary>
/// Request DTO for employee self check-in.
/// Both fields are optional; server defaults to UtcNow if not provided.
/// </summary>
public sealed record CheckInRequest(
    DateTime? CheckInTimeUtc = null,
    string? Notes = null
);

/// <summary>
/// Request DTO for employee self check-out.
/// </summary>
public sealed record CheckOutRequest(
    DateTime? CheckOutTimeUtc = null,
    string? Notes = null
);

/// <summary>
/// Request DTO for HR / manager recording attendance manually.
/// </summary>
public sealed record RecordManualAttendanceRequest(
    Guid EmployeeId,
    DateOnly Date,
    DateTime CheckInTimeUtc,
    DateTime CheckOutTimeUtc,
    Guid? CompanyId = null,
    string? Notes = null
);

/// <summary>
/// Request DTO for HR updating an existing attendance record.
/// </summary>
public sealed record UpdateAttendanceRequest(
    DateTime CheckInTimeUtc,
    DateTime? CheckOutTimeUtc = null,
    string? Notes = null
);

// ─── Shift Contracts ─────────────────────────────────────────────────────────

public sealed record CreateShiftRequest(
    string Name,
    TimeOnly StartTime,
    TimeOnly EndTime,
    Guid? CompanyId = null,
    string? Description = null
);

public sealed record UpdateShiftRequest(
    string Name,
    TimeOnly StartTime,
    TimeOnly EndTime,
    string? Description = null
);

public sealed record AssignShiftRequest(
    Guid ShiftId,
    Guid EmployeeId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo = null,
    Guid? CompanyId = null
);

// ─── Leave Contracts ─────────────────────────────────────────────────────────

public sealed record CreateLeaveTypeRequest(
    string Name,
    int DefaultDaysPerYear,
    bool IsPaid = true,
    string? Description = null
);

public sealed record SubmitLeaveRequestRequest(
    Guid LeaveTypeId,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Reason = null
);

public sealed record ApproveLeaveRequestRequest(
    bool IsApproved,
    string? Notes = null
);
