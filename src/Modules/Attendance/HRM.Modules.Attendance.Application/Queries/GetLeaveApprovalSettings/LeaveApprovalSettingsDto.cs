namespace HRM.Modules.Attendance.Application.Queries.GetLeaveApprovalSettings;

public sealed class LeaveApprovalSettingsDto
{
    public bool RequiresApproval { get; init; }
    public int MaxApprovalLevels { get; init; }
    public int? AutoApproveIfDaysLessThanOrEqual { get; init; }
    public bool AllowSelfCancel { get; init; }
    public bool NotifyOnDecision { get; init; }
}
