using HRM.Web.Models;

namespace HRM.Web.Services.Abstractions;

public interface IAttendanceApiClient
{
    Task<ApiResponse<Guid>> CheckInAsync(
        DateTime? checkInTimeUtc = null,
        string? notes = null,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<Guid>> CheckOutAsync(
        DateTime? checkOutTimeUtc = null,
        string? notes = null,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PagedResult<AttendanceSummaryResponse>>> GetMyAttendanceAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PagedResult<AttendanceSummaryResponse>>> GetEmployeeAttendanceAsync(
        Guid employeeId,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<AttendanceDetailResponse>> GetAttendanceByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<Guid>> RecordManualAttendanceAsync(
        Guid employeeId,
        DateOnly date,
        DateTime checkInTimeUtc,
        DateTime checkOutTimeUtc,
        Guid? companyId = null,
        string? notes = null,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<object>> UpdateAttendanceAsync(
        Guid id,
        DateTime checkInTimeUtc,
        DateTime? checkOutTimeUtc = null,
        string? notes = null,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<object>> DeleteAttendanceAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PagedResult<AttendanceSummaryResponse>>> GetTeamAttendanceAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<IReadOnlyList<AttendanceDailySummaryResponse>>> GetAttendanceSummaryAsync(
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default);

    // ─── Shifts ──────────────────────────────────────────────────────────────

    Task<ApiResponse<PagedResult<ShiftResponse>>> GetShiftsAsync(
        bool? isActive = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<Guid>> CreateShiftAsync(
        string name, TimeOnly startTime, TimeOnly endTime,
        Guid? companyId = null, string? description = null,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<object>> UpdateShiftAsync(
        Guid id, string name, TimeOnly startTime, TimeOnly endTime,
        string? description = null,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<object>> DeleteShiftAsync(
        Guid id, CancellationToken cancellationToken = default);

    // ─── Shift Assignments ───────────────────────────────────────────────────

    Task<ApiResponse<PagedResult<ShiftAssignmentResponse>>> GetShiftAssignmentsAsync(
        Guid? employeeId = null, Guid? shiftId = null,
        int pageNumber = 1, int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<Guid>> AssignShiftAsync(
        Guid shiftId, Guid employeeId, DateOnly effectiveFrom,
        DateOnly? effectiveTo = null, Guid? companyId = null,
        CancellationToken cancellationToken = default);

    // ─── Leave Types ─────────────────────────────────────────────────────────

    Task<ApiResponse<IReadOnlyList<LeaveTypeResponse>>> GetLeaveTypesAsync(
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<Guid>> CreateLeaveTypeAsync(
        string name, int defaultDaysPerYear, bool isPaid = true,
        string? description = null,
        CancellationToken cancellationToken = default);

    // ─── Leave Requests ──────────────────────────────────────────────────────

    Task<ApiResponse<PagedResult<LeaveRequestResponse>>> GetLeaveRequestsAsync(
        Guid? employeeId = null, string? status = null,
        int pageNumber = 1, int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<Guid>> SubmitLeaveRequestAsync(
        Guid leaveTypeId, DateOnly startDate, DateOnly endDate,
        string? reason = null,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<object>> ApproveLeaveRequestAsync(
        Guid leaveRequestId, bool isApproved, string? notes = null,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<object>> CancelLeaveRequestAsync(
        Guid leaveRequestId,
        CancellationToken cancellationToken = default);

    // ─── Leave Approval Settings ──────────────────────────────────────────────

    Task<ApiResponse<LeaveApprovalSettingsResponse>> GetLeaveApprovalSettingsAsync(
        CancellationToken cancellationToken = default);

    Task<ApiResponse<object>> UpdateLeaveApprovalSettingsAsync(
        bool requiresApproval,
        int maxApprovalLevels,
        int? autoApproveIfDaysLessThanOrEqual,
        bool allowSelfCancel,
        bool notifyOnDecision,
        CancellationToken cancellationToken = default);
}
