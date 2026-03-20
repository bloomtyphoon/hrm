using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Attendance.Application.Commands.DeleteShift;

public sealed record DeleteShiftCommand(Guid ShiftId) : IModuleCommand
{
    public string ModuleName => "Attendance";
}
