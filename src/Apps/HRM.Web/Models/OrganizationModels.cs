namespace HRM.Web.Models;

/// <summary>
/// Response model for department data.
/// </summary>
public sealed class DepartmentResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public Guid? ParentDepartmentId { get; set; }
    public Guid? ManagerId { get; set; }
    public int Level { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
}

/// <summary>
/// Response model for position data.
/// </summary>
public sealed class PositionResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? DepartmentId { get; set; }
    public int PositionLevel { get; set; }
    public bool IsManagement { get; set; }
    public int? MaxHeadcount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
}

/// <summary>
/// View model for department list page.
/// </summary>
public sealed class DepartmentListViewModel
{
    public IReadOnlyList<DepartmentResponse> Departments { get; set; } = [];
    public string? SearchTerm { get; set; }
    public string? StatusFilter { get; set; }
    public Guid? CompanyId { get; set; }
    public bool RequiresCompanySelection { get; set; }
}

/// <summary>
/// View model for position list page.
/// </summary>
public sealed class PositionListViewModel
{
    public IReadOnlyList<PositionResponse> Positions { get; set; } = [];
    public string? SearchTerm { get; set; }
    public string? StatusFilter { get; set; }
    public Guid? CompanyId { get; set; }
    public bool RequiresCompanySelection { get; set; }
}
