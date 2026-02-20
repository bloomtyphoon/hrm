namespace HRM.Web.Services.Abstractions;

/// <summary>
/// Provides the currently selected company context for the authenticated user.
/// System accounts can select "All Companies" (null) or a specific company.
/// Employee accounts always have a specific company selected (default = PrimaryCompanyId).
/// </summary>
public interface ICompanyContext
{
    /// <summary>
    /// Gets the selected company ID. Null means "All Companies" (System account only).
    /// </summary>
    Guid? SelectedCompanyId { get; }

    /// <summary>
    /// Whether "All Companies" is selected (System account only).
    /// </summary>
    bool IsAllCompanies { get; }

    /// <summary>
    /// Sets the selected company ID. Pass null for "All Companies" (System only).
    /// </summary>
    void SetSelectedCompanyId(Guid? companyId);
}
