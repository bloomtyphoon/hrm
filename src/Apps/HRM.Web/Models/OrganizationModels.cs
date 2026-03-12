using System.ComponentModel.DataAnnotations;

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
/// View model for department details page with resolved names.
/// </summary>
public sealed class DepartmentDetailViewModel
{
    public DepartmentResponse Department { get; set; } = new();
    public string? ParentDepartmentName { get; set; }
    public string? ManagerName { get; set; }
    public IReadOnlyList<EmployeeSummaryResponse> AvailableEmployees { get; set; } = [];
    public IReadOnlyList<DepartmentResponse> AvailableDepartments { get; set; } = [];
}

/// <summary>
/// View model for position details page with resolved names.
/// </summary>
public sealed class PositionDetailViewModel
{
    public PositionResponse Position { get; set; } = new();
    public string? DepartmentName { get; set; }
    public IReadOnlyList<DepartmentResponse> AvailableDepartments { get; set; } = [];
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

/// <summary>
/// Form model for creating a department.
/// </summary>
public sealed class CreateDepartmentFormModel
{
    public Guid CompanyId { get; set; }

    [Required(ErrorMessage = "Department code is required")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Code must be between 1 and 50 characters")]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Code can only contain letters, numbers, underscores, and hyphens")]
    [Display(Name = "Department Code")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Department name is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 200 characters")]
    [Display(Name = "Department Name")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Parent Department")]
    public Guid? ParentDepartmentId { get; set; }
}

/// <summary>
/// Form model for editing a department.
/// </summary>
public sealed class EditDepartmentFormModel
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Department name is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 200 characters")]
    [Display(Name = "Department Name")]
    public string Name { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Form model for creating a position.
/// </summary>
public sealed class CreatePositionFormModel
{
    public Guid CompanyId { get; set; }

    [Required(ErrorMessage = "Position code is required")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Code must be between 1 and 50 characters")]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Code can only contain letters, numbers, underscores, and hyphens")]
    [Display(Name = "Position Code")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Position title is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters")]
    [Display(Name = "Position Title")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Description")]
    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    [Display(Name = "Department")]
    public Guid? DepartmentId { get; set; }

    [Required(ErrorMessage = "Position level is required")]
    [Range(1, 20, ErrorMessage = "Position level must be between 1 and 20")]
    [Display(Name = "Position Level")]
    public int PositionLevel { get; set; } = 1;

    [Display(Name = "Management Position")]
    public bool IsManagement { get; set; }

    [Display(Name = "Max Headcount")]
    [Range(1, 10000, ErrorMessage = "Max headcount must be between 1 and 10,000")]
    public int? MaxHeadcount { get; set; }
}

/// <summary>
/// Form model for editing a position.
/// </summary>
public sealed class EditPositionFormModel
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Position title is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Title must be between 1 and 200 characters")]
    [Display(Name = "Position Title")]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Description")]
    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Position level is required")]
    [Range(1, 20, ErrorMessage = "Position level must be between 1 and 20")]
    [Display(Name = "Position Level")]
    public int PositionLevel { get; set; }

    [Display(Name = "Management Position")]
    public bool IsManagement { get; set; }

    [Display(Name = "Max Headcount")]
    [Range(1, 10000, ErrorMessage = "Max headcount must be between 1 and 10,000")]
    public int? MaxHeadcount { get; set; }

    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Form model for editing a company.
/// </summary>
public sealed class EditCompanyFormModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Company name is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Company name must be between 1 and 200 characters")]
    [Display(Name = "Company Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "Tax ID cannot exceed 50 characters")]
    [Display(Name = "Tax ID")]
    public string? TaxId { get; set; }

    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// View model for creating a department, includes parent department options.
/// </summary>
public sealed class CreateDepartmentViewModel
{
    public CreateDepartmentFormModel Form { get; set; } = new();
    public IReadOnlyList<DepartmentResponse> AvailableParentDepartments { get; set; } = [];
}

/// <summary>
/// View model for creating a position, includes department options.
/// </summary>
public sealed class CreatePositionViewModel
{
    public CreatePositionFormModel Form { get; set; } = new();
    public IReadOnlyList<DepartmentResponse> AvailableDepartments { get; set; } = [];
}
