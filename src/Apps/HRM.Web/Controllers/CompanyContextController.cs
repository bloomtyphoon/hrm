using HRM.Web.Services.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Web.Controllers;

/// <summary>
/// Handles global company context switching from the navbar dropdown.
/// </summary>
[Authorize]
public class CompanyContextController : Controller
{
    private readonly ICompanyContext _companyContext;

    public CompanyContextController(ICompanyContext companyContext)
    {
        _companyContext = companyContext;
    }

    /// <summary>
    /// Switch the active company context.
    /// POST: /CompanyContext/Switch
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Switch(Guid? companyId, string? returnUrl = null)
    {
        _companyContext.SetSelectedCompanyId(companyId);

        // Redirect back to the page the user was on
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }
}
