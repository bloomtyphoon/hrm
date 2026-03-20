using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Attendance.Application.Commands.CreateShift;

public sealed record CreateShiftCommand(
    string Name,
    TimeOnly StartTime,
    TimeOnly EndTime,
    Guid? CompanyId = null,
    string? Description = null
) : IModuleCommand<Guid>
{
    public string ModuleName => "Attendance";
}
