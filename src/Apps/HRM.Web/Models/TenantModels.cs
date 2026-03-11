using System.ComponentModel.DataAnnotations;

namespace HRM.Web.Models;

/// <summary>
/// API response model for Tenant.
/// </summary>
public class TenantResponse
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsSystemTenant { get; set; }
    public string? Subdomain { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ModifiedAtUtc { get; set; }
}

/// <summary>
/// View model for the Tenant list page.
/// </summary>
public class TenantListViewModel
{
    public IReadOnlyList<TenantResponse> Tenants { get; set; } = [];
    public string? SearchTerm { get; set; }
    public string? StatusFilter { get; set; }
}

/// <summary>
/// Form model for creating a new tenant.
/// </summary>
public class CreateTenantRequest
{
    [Required(ErrorMessage = "Code is required.")]
    [StringLength(50, MinimumLength = 1, ErrorMessage = "Code must be between 1 and 50 characters.")]
    [RegularExpression(@"^[A-Za-z0-9_\-]+$", ErrorMessage = "Code can only contain letters, numbers, underscores, and hyphens.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 200 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(63, MinimumLength = 3, ErrorMessage = "Subdomain must be between 3 and 63 characters.")]
    [RegularExpression(@"^[a-z0-9]([a-z0-9\-]{1,61}[a-z0-9])?$",
        ErrorMessage = "Subdomain may only contain lowercase letters, numbers, and hyphens, and cannot start or end with a hyphen.")]
    public string? Subdomain { get; set; }
}

/// <summary>
/// Form model for editing an existing tenant.
/// </summary>
public class EditTenantFormModel
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsSystemTenant { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Name must be between 1 and 200 characters.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(63, MinimumLength = 3, ErrorMessage = "Subdomain must be between 3 and 63 characters.")]
    [RegularExpression(@"^[a-z0-9]([a-z0-9\-]{1,61}[a-z0-9])?$",
        ErrorMessage = "Subdomain may only contain lowercase letters, numbers, and hyphens, and cannot start or end with a hyphen.")]
    public string? Subdomain { get; set; }
}
