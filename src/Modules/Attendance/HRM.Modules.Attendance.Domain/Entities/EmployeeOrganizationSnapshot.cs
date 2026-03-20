namespace HRM.Modules.Attendance.Domain.Entities;

/// <summary>
/// Local read model that caches employee organizational hierarchy data.
/// Kept in sync via integration events from Personnel module.
/// Used by ApprovalChainResolver to avoid cross-module queries.
/// </summary>
public class EmployeeOrganizationSnapshot
{
    public Guid EmployeeId { get; set; }
    public Guid? ManagerId { get; set; }
    public Guid? PrimaryDepartmentId { get; set; }
    public Guid? PrimaryCompanyId { get; set; }

    /// <summary>
    /// UTC timestamp of the last integration event that updated this snapshot.
    /// Used for debugging and staleness detection.
    /// </summary>
    public DateTime LastUpdatedUtc { get; set; }
}
