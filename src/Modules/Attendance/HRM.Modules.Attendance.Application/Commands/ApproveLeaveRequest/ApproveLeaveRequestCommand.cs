using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Attendance.Application.Commands.ApproveLeaveRequest;

public sealed record ApproveLeaveRequestCommand(
    Guid LeaveRequestId,
    bool IsApproved,
    string? Notes = null
) : IModuleCommand
{
    public string ModuleName => "Attendance";
}
