using System.ComponentModel.DataAnnotations;

namespace HRM.Web.Models;

/// <summary>
/// Request model for creating a company.
/// Used for form binding in MVC views.
/// </summary>
public sealed class CreateCompanyRequest
{
    /// <summary>
    /// Company code (unique identifier).
    /// </summary>
    [Required(ErrorMessage = "Company code is required")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Company code must be between 1 and 50 characters")]
    [RegularExpression(@"^[a-zA-Z0-9_-]+$", ErrorMessage = "Company code can only contain letters, numbers, underscores, and hyphens")]
    [Display(Name = "Company Code")]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Company name.
    /// </summary>
    [Required(ErrorMessage = "Company name is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Company name must be between 1 and 200 characters")]
    [Display(Name = "Company Name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Tax identification number (optional).
    /// </summary>
    [StringLength(50, ErrorMessage = "Tax ID cannot exceed 50 characters")]
    [Display(Name = "Tax ID")]
    public string? TaxId { get; set; }
}

/// <summary>
/// Response model for company data.
/// Used for displaying company information in views.
/// </summary>
public sealed class CompanyResponse
{
    /// <summary>
    /// Company ID.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Company code.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Company name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Tax identification number.
    /// </summary>
    public string? TaxId { get; set; }

    /// <summary>
    /// Company status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Creation timestamp.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public DateTime? ModifiedAtUtc { get; set; }
}

/// <summary>
/// View model for company list page.
/// </summary>
public sealed class CompanyListViewModel
{
    /// <summary>
    /// List of companies.
    /// </summary>
    public IReadOnlyList<CompanyResponse> Companies { get; set; } = [];

    /// <summary>
    /// Search term for filtering.
    /// </summary>
    public string? SearchTerm { get; set; }

    /// <summary>
    /// Status filter.
    /// </summary>
    public string? StatusFilter { get; set; }
}
