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
}
