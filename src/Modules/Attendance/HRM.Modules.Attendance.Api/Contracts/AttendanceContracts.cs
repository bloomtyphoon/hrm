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
