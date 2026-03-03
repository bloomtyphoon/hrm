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
}
