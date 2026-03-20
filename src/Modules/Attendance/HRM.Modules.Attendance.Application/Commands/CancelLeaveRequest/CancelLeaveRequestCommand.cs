using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Attendance.Application.Commands.CancelLeaveRequest;

public sealed record CancelLeaveRequestCommand(Guid LeaveRequestId) : IModuleCommand
{
    public string ModuleName => "Attendance";
}
