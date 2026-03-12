using HRM.Web.Models;
using HRM.Web.Services.Abstractions;

namespace HRM.Web.Services.Personnel;

public sealed class PersonnelApiClient(
    HttpClient httpClient,
    ILogger<PersonnelApiClient> logger)
    : ApiClientBase(httpClient, logger), IPersonnelApiClient
{
    public async Task<ApiResponse<PagedResult<EmployeeSummaryResponse>>> GetEmployeesAsync(
        string? searchTerm = null,
        string? status = null,
        Guid? companyId = null,
        Guid? departmentId = null,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string> { $"pageNumber={pageNumber}", $"pageSize={pageSize}" };
        if (!string.IsNullOrWhiteSpace(searchTerm))
            queryParams.Add($"searchTerm={Uri.EscapeDataString(searchTerm)}");
        if (!string.IsNullOrWhiteSpace(status))
            queryParams.Add($"status={Uri.EscapeDataString(status)}");
        if (companyId.HasValue)
            queryParams.Add($"companyId={companyId.Value}");
        if (departmentId.HasValue)
            queryParams.Add($"departmentId={departmentId.Value}");

        var url = $"/api/personnel/employees?{string.Join("&", queryParams)}";
        var response = await HttpClient.GetAsync(url, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<PagedResult<EmployeeSummaryResponse>>(JsonOptions, cancellationToken);
            return new ApiResponse<PagedResult<EmployeeSummaryResponse>>
            {
                IsSuccess = true,
                Data = data ?? new PagedResult<EmployeeSummaryResponse>()
            };
        }
        return await HandleErrorResponseAsync<PagedResult<EmployeeSummaryResponse>>(response, "Failed to retrieve employees", cancellationToken);
    }

    public Task<ApiResponse<EmployeeDetailResponse>> GetEmployeeByIdAsync(
        Guid id, CancellationToken cancellationToken = default)
        => GetAsync<EmployeeDetailResponse>($"/api/personnel/employees/{id}", "Failed to retrieve employee", cancellationToken);

    public Task<ApiResponse<EmployeeDetailResponse>> CreateEmployeeAsync(
        string employeeCode, string firstName, string lastName, string email,
        DateOnly hireDate, string? phone, DateOnly? dateOfBirth,
        Guid? managerId = null,
        CancellationToken cancellationToken = default)
        => PostAsync<EmployeeDetailResponse>("/api/personnel/employees",
            new
            {
                EmployeeCode = employeeCode,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                HireDate = hireDate,
                Phone = phone,
                DateOfBirth = dateOfBirth,
                ManagerId = managerId
            },
            "Failed to create employee", cancellationToken);

    public Task<ApiResponse<object>> UpdateEmployeeAsync(
        Guid id, string firstName, string lastName, string email,
        string? phone, DateOnly? dateOfBirth,
        CancellationToken cancellationToken = default)
        => PutAsync<object>($"/api/personnel/employees/{id}",
            new { FirstName = firstName, LastName = lastName, Email = email, Phone = phone, DateOfBirth = dateOfBirth },
            "Failed to update employee", cancellationToken);

    public Task<ApiResponse<object>> TerminateEmployeeAsync(
        Guid id, DateOnly terminationDate,
        CancellationToken cancellationToken = default)
        => PutAsync<object>($"/api/personnel/employees/{id}/terminate",
            new { TerminationDate = terminationDate },
            "Failed to terminate employee", cancellationToken);

    // ─── Manager ──────────────────────────────────────────────────────────

    public Task<ApiResponse<object>> AssignManagerAsync(
        Guid employeeId, Guid managerId,
        CancellationToken cancellationToken = default)
        => PutAsync<object>($"/api/personnel/employees/{employeeId}/manager",
            new { ManagerId = managerId },
            "Failed to assign manager", cancellationToken);

    public Task<ApiResponse<object>> RemoveManagerAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default)
        => DeleteAsync<object>($"/api/personnel/employees/{employeeId}/manager",
            "Failed to remove manager", cancellationToken);

    public async Task<ApiResponse<PagedResult<EmployeeSummaryResponse>>> GetDirectReportsAsync(
        Guid managerId, int pageNumber = 1, int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var url = $"/api/personnel/employees/{managerId}/direct-reports?pageNumber={pageNumber}&pageSize={pageSize}";
        var response = await HttpClient.GetAsync(url, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<PagedResult<EmployeeSummaryResponse>>(JsonOptions, cancellationToken);
            return new ApiResponse<PagedResult<EmployeeSummaryResponse>>
            {
                IsSuccess = true,
                Data = data ?? new PagedResult<EmployeeSummaryResponse>()
            };
        }
        return await HandleErrorResponseAsync<PagedResult<EmployeeSummaryResponse>>(response, "Failed to retrieve direct reports", cancellationToken);
    }

    // ─── Assignments ──────────────────────────────────────────────────────

    public async Task<ApiResponse<IReadOnlyList<AssignmentResponse>>> GetAssignmentsAsync(
        Guid employeeId, string? status = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"/api/personnel/employees/{employeeId}/assignments";
        if (!string.IsNullOrWhiteSpace(status))
            url += $"?status={Uri.EscapeDataString(status)}";

        var response = await HttpClient.GetAsync(url, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<List<AssignmentResponse>>(JsonOptions, cancellationToken);
            return new ApiResponse<IReadOnlyList<AssignmentResponse>> { IsSuccess = true, Data = data ?? [] };
        }
        return await HandleErrorResponseAsync<IReadOnlyList<AssignmentResponse>>(response, "Failed to retrieve assignments", cancellationToken);
    }

    public Task<ApiResponse<AssignmentResponse>> AddAssignmentAsync(
        Guid employeeId, Guid companyId, Guid departmentId, Guid positionId,
        DateOnly startDate, bool isPrimary,
        CancellationToken cancellationToken = default)
        => PostAsync<AssignmentResponse>($"/api/personnel/employees/{employeeId}/assignments",
            new { CompanyId = companyId, DepartmentId = departmentId, PositionId = positionId, StartDate = startDate, IsPrimary = isPrimary },
            "Failed to add assignment", cancellationToken);

    public Task<ApiResponse<object>> EndAssignmentAsync(
        Guid employeeId, Guid assignmentId, DateOnly endDate,
        CancellationToken cancellationToken = default)
        => PutAsync<object>($"/api/personnel/employees/{employeeId}/assignments/{assignmentId}/end",
            new { EndDate = endDate },
            "Failed to end assignment", cancellationToken);

    public Task<ApiResponse<object>> SetPrimaryAssignmentAsync(
        Guid employeeId, Guid assignmentId,
        CancellationToken cancellationToken = default)
        => PutAsync<object>($"/api/personnel/employees/{employeeId}/assignments/{assignmentId}/primary",
            new { },
            "Failed to set primary assignment", cancellationToken);
}
