using HRM.Web.Models;
using HRM.Web.Services.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Web.ViewComponents;

/// <summary>
/// ViewComponent that renders the company switcher dropdown in the navbar.
/// Loads the company list from the API and displays based on account type.
/// </summary>
public class CompanySwitcherViewComponent : ViewComponent
{
    private readonly IOrganizationApiClient _organizationClient;
    private readonly ICompanyContext _companyContext;

    public CompanySwitcherViewComponent(
        IOrganizationApiClient organizationClient,
        ICompanyContext companyContext)
    {
        _organizationClient = organizationClient;
        _companyContext = companyContext;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var accountType = HttpContext.User.FindFirst("AccountType")?.Value ?? "Employee";
        var isSystem = accountType == "System";

        // Load companies
        var companiesResponse = await _organizationClient.GetCompaniesAsync();
        var companies = companiesResponse.IsSuccess && companiesResponse.Data != null
            ? companiesResponse.Data.Where(c => c.Status == "Active").ToList()
            : new List<CompanyResponse>();

        var selectedCompanyId = _companyContext.SelectedCompanyId;
        var isAllCompanies = _companyContext.IsAllCompanies;

        // Find the selected company name for display
        string displayName;
        if (isAllCompanies)
        {
            displayName = "All Companies";
        }
        else if (selectedCompanyId.HasValue)
        {
            var selected = companies.FirstOrDefault(c => c.Id == selectedCompanyId.Value);
            displayName = selected?.Name ?? "Select Company";
        }
        else
        {
            displayName = isSystem ? "All Companies" : "Select Company";
        }

        var model = new CompanySwitcherViewModel
        {
            Companies = companies,
            SelectedCompanyId = selectedCompanyId,
            IsAllCompanies = isAllCompanies || (!selectedCompanyId.HasValue && isSystem),
            IsSystemAccount = isSystem,
            DisplayName = displayName,
            ReturnUrl = HttpContext.Request.Path + HttpContext.Request.QueryString
        };

        return View(model);
    }
}
