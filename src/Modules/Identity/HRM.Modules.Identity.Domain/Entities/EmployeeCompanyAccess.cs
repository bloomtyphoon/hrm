namespace HRM.Modules.Identity.Domain.Entities;

/// <summary>
/// Represents a company that an employee has access to.
/// Child entity of EmployeeProfile — stored in Identity.EmployeeProfileCompanies table.
///
/// This is a denormalized copy of company assignments from Personnel module.
/// Synced via:
/// - Admin API when creating/updating EmployeeProfile
/// - Integration events from Personnel module when assignments change (future)
///
/// Purpose: Allow Identity module to resolve company-based visibility
/// without cross-module queries to personnel.EmployeeAssignments.
/// </summary>
public sealed class EmployeeCompanyAccess
{
    /// <summary>
    /// Company ID (opaque reference to Organization module)
    /// </summary>
    public Guid CompanyId { get; private set; }

    private EmployeeCompanyAccess() { }

    public static EmployeeCompanyAccess Create(Guid companyId)
    {
        return new EmployeeCompanyAccess { CompanyId = companyId };
    }
}
