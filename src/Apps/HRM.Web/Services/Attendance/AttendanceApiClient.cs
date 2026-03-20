using HRM.Web.Models;
using HRM.Web.Services.Abstractions;

namespace HRM.Web.Services.Attendance;

public sealed class AttendanceApiClient(
    HttpClient httpClient,
    ILogger<AttendanceApiClient> logger)
    : ApiClientBase(httpClient, logger), IAttendanceApiClient
{
    public Task<ApiResponse<Guid>> CheckInAsync(
        DateTime? checkInTimeUtc = null,
        string? notes = null,
        CancellationToken cancellationToken = default)
        => PostAsync<Guid>("/api/attendance/check-in",
            new { CheckInTimeUtc = checkInTimeUtc, Notes = notes },
            "Failed to check in", cancellationToken);

    public Task<ApiResponse<Guid>> CheckOutAsync(
        DateTime? checkOutTimeUtc = null,
        string? notes = null,
        CancellationToken cancellationToken = default)
        => PostAsync<Guid>("/api/attendance/check-out",
            new { CheckOutTimeUtc = checkOutTimeUtc, Notes = notes },
            "Failed to check out", cancellationToken);

    public async Task<ApiResponse<PagedResult<AttendanceSummaryResponse>>> GetMyAttendanceAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string> { $"pageNumber={pageNumber}", $"pageSize={pageSize}" };
        if (fromDate.HasValue) queryParams.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
        if (toDate.HasValue) queryParams.Add($"toDate={toDate.Value:yyyy-MM-dd}");

        return await GetAsync<PagedResult<AttendanceSummaryResponse>>(
            $"/api/attendance/me?{string.Join("&", queryParams)}",
            "Failed to retrieve attendance records", cancellationToken);
    }

    public async Task<ApiResponse<PagedResult<AttendanceSummaryResponse>>> GetEmployeeAttendanceAsync(
        Guid employeeId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string> { $"pageNumber={pageNumber}", $"pageSize={pageSize}" };
        if (fromDate.HasValue) queryParams.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
        if (toDate.HasValue) queryParams.Add($"toDate={toDate.Value:yyyy-MM-dd}");

        return await GetAsync<PagedResult<AttendanceSummaryResponse>>(
            $"/api/attendance/employees/{employeeId}?{string.Join("&", queryParams)}",
            "Failed to retrieve employee attendance", cancellationToken);
    }

    public Task<ApiResponse<AttendanceDetailResponse>> GetAttendanceByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => GetAsync<AttendanceDetailResponse>(
            $"/api/attendance/records/{id}",
            "Failed to retrieve attendance record", cancellationToken);

    public Task<ApiResponse<Guid>> RecordManualAttendanceAsync(
        Guid employeeId,
        DateOnly date,
        DateTime checkInTimeUtc,
        DateTime checkOutTimeUtc,
        Guid? companyId = null,
        string? notes = null,
        CancellationToken cancellationToken = default)
        => PostAsync<Guid>("/api/attendance/records",
            new
            {
                EmployeeId = employeeId,
                Date = date,
                CheckInTimeUtc = checkInTimeUtc,
                CheckOutTimeUtc = checkOutTimeUtc,
                CompanyId = companyId,
                Notes = notes
            },
            "Failed to record manual attendance", cancellationToken);

    public Task<ApiResponse<object>> UpdateAttendanceAsync(
        Guid id,
        DateTime checkInTimeUtc,
        DateTime? checkOutTimeUtc = null,
        string? notes = null,
        CancellationToken cancellationToken = default)
        => PutAsync<object>($"/api/attendance/records/{id}",
            new { CheckInTimeUtc = checkInTimeUtc, CheckOutTimeUtc = checkOutTimeUtc, Notes = notes },
            "Failed to update attendance record", cancellationToken);

    public Task<ApiResponse<object>> DeleteAttendanceAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => DeleteAsync<object>($"/api/attendance/records/{id}",
            "Failed to delete attendance record", cancellationToken);

    public async Task<ApiResponse<PagedResult<AttendanceSummaryResponse>>> GetTeamAttendanceAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string> { $"pageNumber={pageNumber}", $"pageSize={pageSize}" };
        if (fromDate.HasValue) queryParams.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
        if (toDate.HasValue) queryParams.Add($"toDate={toDate.Value:yyyy-MM-dd}");

        return await GetAsync<PagedResult<AttendanceSummaryResponse>>(
            $"/api/attendance/team?{string.Join("&", queryParams)}",
            "Failed to retrieve team attendance records", cancellationToken);
    }

    public async Task<ApiResponse<IReadOnlyList<AttendanceDailySummaryResponse>>> GetAttendanceSummaryAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();
        if (fromDate.HasValue) queryParams.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
        if (toDate.HasValue) queryParams.Add($"toDate={toDate.Value:yyyy-MM-dd}");

        var url = queryParams.Count > 0
            ? $"/api/attendance/summary?{string.Join("&", queryParams)}"
            : "/api/attendance/summary";

        return await GetAsync<IReadOnlyList<AttendanceDailySummaryResponse>>(
            url, "Failed to retrieve attendance summary", cancellationToken);
    }

    // ─── Shifts ──────────────────────────────────────────────────────────────

    public async Task<ApiResponse<PagedResult<ShiftResponse>>> GetShiftsAsync(
        bool? isActive = null, int pageNumber = 1, int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string> { $"pageNumber={pageNumber}", $"pageSize={pageSize}" };
        if (isActive.HasValue) queryParams.Add($"isActive={isActive.Value}");

        return await GetAsync<PagedResult<ShiftResponse>>(
            $"/api/attendance/shifts?{string.Join("&", queryParams)}",
            "Failed to retrieve shifts", cancellationToken);
    }

    public Task<ApiResponse<Guid>> CreateShiftAsync(
        string name, TimeOnly startTime, TimeOnly endTime,
        Guid? companyId = null, string? description = null,
        CancellationToken cancellationToken = default)
        => PostAsync<Guid>("/api/attendance/shifts",
            new { Name = name, StartTime = startTime, EndTime = endTime, CompanyId = companyId, Description = description },
            "Failed to create shift", cancellationToken);

    public Task<ApiResponse<object>> UpdateShiftAsync(
        Guid id, string name, TimeOnly startTime, TimeOnly endTime,
        string? description = null,
        CancellationToken cancellationToken = default)
        => PutAsync<object>($"/api/attendance/shifts/{id}",
            new { Name = name, StartTime = startTime, EndTime = endTime, Description = description },
            "Failed to update shift", cancellationToken);

    public Task<ApiResponse<object>> DeleteShiftAsync(
        Guid id, CancellationToken cancellationToken = default)
        => DeleteAsync<object>($"/api/attendance/shifts/{id}",
            "Failed to delete shift", cancellationToken);

    // ─── Shift Assignments ───────────────────────────────────────────────────

    public async Task<ApiResponse<PagedResult<ShiftAssignmentResponse>>> GetShiftAssignmentsAsync(
        Guid? employeeId = null, Guid? shiftId = null,
        int pageNumber = 1, int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string> { $"pageNumber={pageNumber}", $"pageSize={pageSize}" };
        if (employeeId.HasValue) queryParams.Add($"employeeId={employeeId.Value}");
        if (shiftId.HasValue) queryParams.Add($"shiftId={shiftId.Value}");

        return await GetAsync<PagedResult<ShiftAssignmentResponse>>(
            $"/api/attendance/shift-assignments?{string.Join("&", queryParams)}",
            "Failed to retrieve shift assignments", cancellationToken);
    }

    public Task<ApiResponse<Guid>> AssignShiftAsync(
        Guid shiftId, Guid employeeId, DateOnly effectiveFrom,
        DateOnly? effectiveTo = null, Guid? companyId = null,
        CancellationToken cancellationToken = default)
        => PostAsync<Guid>("/api/attendance/shift-assignments",
            new { ShiftId = shiftId, EmployeeId = employeeId, EffectiveFrom = effectiveFrom, EffectiveTo = effectiveTo, CompanyId = companyId },
            "Failed to assign shift", cancellationToken);

    // ─── Leave Types ─────────────────────────────────────────────────────────

    public async Task<ApiResponse<IReadOnlyList<LeaveTypeResponse>>> GetLeaveTypesAsync(
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var url = isActive.HasValue
            ? $"/api/attendance/leave-types?isActive={isActive.Value}"
            : "/api/attendance/leave-types";

        return await GetAsync<IReadOnlyList<LeaveTypeResponse>>(
            url, "Failed to retrieve leave types", cancellationToken);
    }

    public Task<ApiResponse<Guid>> CreateLeaveTypeAsync(
        string name, int defaultDaysPerYear, bool isPaid = true,
        string? description = null,
        CancellationToken cancellationToken = default)
        => PostAsync<Guid>("/api/attendance/leave-types",
            new { Name = name, DefaultDaysPerYear = defaultDaysPerYear, IsPaid = isPaid, Description = description },
            "Failed to create leave type", cancellationToken);

    // ─── Leave Requests ──────────────────────────────────────────────────────

    public async Task<ApiResponse<PagedResult<LeaveRequestResponse>>> GetLeaveRequestsAsync(
        Guid? employeeId = null, string? status = null,
        int pageNumber = 1, int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string> { $"pageNumber={pageNumber}", $"pageSize={pageSize}" };
        if (employeeId.HasValue) queryParams.Add($"employeeId={employeeId.Value}");
        if (!string.IsNullOrWhiteSpace(status)) queryParams.Add($"status={status}");

        return await GetAsync<PagedResult<LeaveRequestResponse>>(
            $"/api/attendance/leave-requests?{string.Join("&", queryParams)}",
            "Failed to retrieve leave requests", cancellationToken);
    }

    public Task<ApiResponse<Guid>> SubmitLeaveRequestAsync(
        Guid leaveTypeId, DateOnly startDate, DateOnly endDate,
        string? reason = null,
        CancellationToken cancellationToken = default)
        => PostAsync<Guid>("/api/attendance/leave-requests",
            new { LeaveTypeId = leaveTypeId, StartDate = startDate, EndDate = endDate, Reason = reason },
            "Failed to submit leave request", cancellationToken);

    public Task<ApiResponse<object>> ApproveLeaveRequestAsync(
        Guid leaveRequestId, bool isApproved, string? notes = null,
        CancellationToken cancellationToken = default)
        => PostAsync<object>($"/api/attendance/leave-requests/{leaveRequestId}/approve",
            new { IsApproved = isApproved, Notes = notes },
            "Failed to approve leave request", cancellationToken);

    public Task<ApiResponse<object>> CancelLeaveRequestAsync(
        Guid leaveRequestId,
        CancellationToken cancellationToken = default)
        => PostAsync<object>($"/api/attendance/leave-requests/{leaveRequestId}/cancel",
            "Failed to cancel leave request", cancellationToken);
}
