namespace HRM.Modules.Attendance.Domain.Entities;

/// <summary>
/// Local read model that caches department manager data.
/// Kept in sync via integration events from Organization module.
/// Used by ApprovalChainResolver to avoid cross-module queries.
/// </summary>
public class DepartmentSnapshot
{
    public Guid DepartmentId { get; set; }
    public Guid? ManagerEmployeeId { get; set; }

    /// <summary>
    /// UTC timestamp of the last integration event that updated this snapshot.
    /// </summary>
    public DateTime LastUpdatedUtc { get; set; }
}
