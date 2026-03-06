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
                DateOfBirth = dateOfBirth
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
}
