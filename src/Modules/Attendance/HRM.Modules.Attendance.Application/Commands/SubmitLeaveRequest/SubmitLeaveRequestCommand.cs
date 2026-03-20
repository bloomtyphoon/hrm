using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Attendance.Application.Commands.SubmitLeaveRequest;

public sealed record SubmitLeaveRequestCommand(
    Guid LeaveTypeId,
    DateOnly StartDate,
    DateOnly EndDate,
    string? Reason = null
) : IModuleCommand<Guid>
{
    public string ModuleName => "Attendance";
}
