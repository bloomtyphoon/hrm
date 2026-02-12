namespace HRM.Web.Models;

/// <summary>
/// View model for the CompanySwitcher ViewComponent.
/// </summary>
public sealed class CompanySwitcherViewModel
{
    public IReadOnlyList<CompanyResponse> Companies { get; set; } = [];
    public Guid? SelectedCompanyId { get; set; }
    public bool IsAllCompanies { get; set; }
    public bool IsSystemAccount { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string ReturnUrl { get; set; } = "/";
}
