using HRM.Web.Models;

namespace HRM.Web.Services.Abstractions;

/// <summary>
/// API client for Personnel module operations.
/// Handles employee management.
/// </summary>
public interface IPersonnelApiClient
{
    /// <summary>
    /// Get paginated list of employees with optional filtering.
    /// </summary>
    Task<ApiResponse<PagedResult<EmployeeSummaryResponse>>> GetEmployeesAsync(
        string? searchTerm = null,
        string? status = null,
        Guid? companyId = null,
        Guid? departmentId = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get employee by ID.
    /// </summary>
    Task<ApiResponse<EmployeeSummaryResponse>> GetEmployeeByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
