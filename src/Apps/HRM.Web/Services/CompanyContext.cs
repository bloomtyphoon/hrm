using System.Security.Claims;
using HRM.Web.Services.Abstractions;

namespace HRM.Web.Services;

/// <summary>
/// Cookie-based company context that persists the selected company across requests.
/// - System account: default = null (all companies), can switch to specific company.
/// - Employee account: default = PrimaryCompanyId (fetched lazily on first access).
/// </summary>
public sealed class CompanyContext : ICompanyContext
{
    private const string CookieName = "HRM.SelectedCompanyId";
    private const string AllCompaniesValue = "all";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CompanyContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? SelectedCompanyId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true)
                return null;

            // Read from cookie
            if (httpContext.Request.Cookies.TryGetValue(CookieName, out var cookieValue))
            {
                if (cookieValue == AllCompaniesValue)
                    return null; // "All Companies" selected

                if (Guid.TryParse(cookieValue, out var companyId))
                    return companyId;
            }

            // No cookie set yet — return null (controllers will handle default logic)
            return null;
        }
    }

    public bool IsAllCompanies
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated != true)
                return false;

            var accountType = httpContext.User.FindFirst("AccountType")?.Value;

            // Employee can never see all companies
            if (accountType != "System")
                return false;

            // Check cookie
            if (httpContext.Request.Cookies.TryGetValue(CookieName, out var cookieValue))
                return cookieValue == AllCompaniesValue;

            // Default for System = All Companies (no cookie means all)
            return true;
        }
    }

    public void SetSelectedCompanyId(Guid? companyId)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null) return;

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(30),
            Path = "/"
        };

        var value = companyId.HasValue ? companyId.Value.ToString() : AllCompaniesValue;
        httpContext.Response.Cookies.Append(CookieName, value, cookieOptions);
    }
}
