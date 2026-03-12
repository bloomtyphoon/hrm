using HRM.Web.Models;

namespace HRM.Web.Services.Abstractions;

public interface IPersonnelApiClient
{
    Task<ApiResponse<PagedResult<EmployeeSummaryResponse>>> GetEmployeesAsync(
        string? searchTerm = null,
        string? status = null,
        Guid? companyId = null,
        Guid? departmentId = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<EmployeeDetailResponse>> GetEmployeeByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<EmployeeDetailResponse>> CreateEmployeeAsync(
        string employeeCode,
        string firstName,
        string lastName,
        string email,
        DateOnly hireDate,
        string? phone,
        DateOnly? dateOfBirth,
        Guid? managerId = null,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<object>> UpdateEmployeeAsync(
        Guid id,
        string firstName,
        string lastName,
        string email,
        string? phone,
        DateOnly? dateOfBirth,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<object>> TerminateEmployeeAsync(
        Guid id,
        DateOnly terminationDate,
        CancellationToken cancellationToken = default);

    // ─── Manager ──────────────────────────────────────────────────────────

    Task<ApiResponse<object>> AssignManagerAsync(
        Guid employeeId,
        Guid managerId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<object>> RemoveManagerAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PagedResult<EmployeeSummaryResponse>>> GetDirectReportsAsync(
        Guid managerId,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    // ─── Assignments ──────────────────────────────────────────────────────

    Task<ApiResponse<IReadOnlyList<AssignmentResponse>>> GetAssignmentsAsync(
        Guid employeeId,
        string? status = null,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<AssignmentResponse>> AddAssignmentAsync(
        Guid employeeId,
        Guid companyId,
        Guid departmentId,
        Guid positionId,
        DateOnly startDate,
        bool isPrimary,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<object>> EndAssignmentAsync(
        Guid employeeId,
        Guid assignmentId,
        DateOnly endDate,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<object>> SetPrimaryAssignmentAsync(
        Guid employeeId,
        Guid assignmentId,
        CancellationToken cancellationToken = default);
}
