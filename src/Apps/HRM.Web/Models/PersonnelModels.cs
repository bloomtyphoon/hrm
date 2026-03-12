using System.ComponentModel.DataAnnotations;

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
/// Response model for employee detail data.
/// </summary>
public sealed class EmployeeDetailResponse
{
    public Guid Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public DateOnly HireDate { get; set; }
    public DateOnly? TerminationDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid? ManagerId { get; set; }
    public Guid? PrimaryCompanyId { get; set; }
    public Guid? PrimaryDepartmentId { get; set; }
    public Guid? PrimaryPositionId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
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

/// <summary>
/// Response model for assignment data.
/// </summary>
public sealed class AssignmentResponse
{
    public Guid Id { get; set; }
    public Guid EmployeeId { get; set; }
    public Guid CompanyId { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid PositionId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsPrimary { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// View model for employee details page with resolved names.
/// </summary>
public sealed class EmployeeDetailViewModel
{
    public EmployeeDetailResponse Employee { get; set; } = new();
    public string? ManagerName { get; set; }
    public string? PrimaryCompanyName { get; set; }
    public string? PrimaryDepartmentName { get; set; }
    public string? PrimaryPositionTitle { get; set; }
    public IReadOnlyList<AssignmentDisplayItem> Assignments { get; set; } = [];
    public IReadOnlyList<EmployeeSummaryResponse> AvailableManagers { get; set; } = [];
    public IReadOnlyList<CompanyResponse> AvailableCompanies { get; set; } = [];
    public IReadOnlyList<DepartmentResponse> AvailableDepartments { get; set; } = [];
    public IReadOnlyList<PositionResponse> AvailablePositions { get; set; } = [];
}

/// <summary>
/// Display item for an assignment with resolved names.
/// </summary>
public sealed class AssignmentDisplayItem
{
    public Guid Id { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public string PositionTitle { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public bool IsPrimary { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Form model for creating an employee.
/// </summary>
public sealed class CreateEmployeeFormModel
{
    [Required(ErrorMessage = "Employee code is required")]
    [StringLength(50, MinimumLength = 1)]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Code can only contain letters, numbers, underscores, and hyphens")]
    [Display(Name = "Employee Code")]
    public string EmployeeCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "First name is required")]
    [StringLength(100, MinimumLength = 1)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100, MinimumLength = 1)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(255)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Hire date is required")]
    [Display(Name = "Hire Date")]
    public DateOnly HireDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [StringLength(20)]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    [Display(Name = "Date of Birth")]
    public DateOnly? DateOfBirth { get; set; }
}

/// <summary>
/// Form model for editing an employee.
/// </summary>
public sealed class EditEmployeeFormModel
{
    public Guid Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "First name is required")]
    [StringLength(100, MinimumLength = 1)]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100, MinimumLength = 1)]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(255)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [StringLength(20)]
    [Display(Name = "Phone")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Hire date is required")]
    [Display(Name = "Hire Date")]
    public DateOnly HireDate { get; set; }

    [Display(Name = "Date of Birth")]
    public DateOnly? DateOfBirth { get; set; }

    public string Status { get; set; } = string.Empty;
}
