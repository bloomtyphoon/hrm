using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Attendance.Application.Commands.UpdateLeaveApprovalSettings;

public sealed record UpdateLeaveApprovalSettingsCommand(
    bool RequiresApproval,
    int MaxApprovalLevels,
    int? AutoApproveIfDaysLessThanOrEqual,
    bool AllowSelfCancel,
    bool NotifyOnDecision
) : IModuleCommand
{
    public string ModuleName => "Attendance";
}
