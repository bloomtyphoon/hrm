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
}
