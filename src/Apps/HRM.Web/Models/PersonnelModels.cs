namespace HRM.Web.Models;

/// <summary>
/// Response model for employee summary data (list view).
/// </summary>
public sealed class EmployeeSummaryResponse
{
    public Guid Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateOnly HireDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? ManagerId { get; set; }
    public Guid? PrimaryCompanyId { get; set; }
    public Guid? PrimaryDepartmentId { get; set; }
    public Guid? PrimaryPositionId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// View model for employee list page.
/// </summary>
public sealed class EmployeeListViewModel
{
    public PagedResult<EmployeeSummaryResponse> Employees { get; set; } = new();
    public string? SearchTerm { get; set; }
    public string? StatusFilter { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
