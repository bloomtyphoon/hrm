using HRM.BuildingBlocks.Application.Abstractions.Commands;

namespace HRM.Modules.Attendance.Application.Commands.AssignShift;

public sealed record AssignShiftCommand(
    Guid ShiftId,
    Guid EmployeeId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo = null,
    Guid? CompanyId = null
) : IModuleCommand<Guid>
{
    public string ModuleName => "Attendance";
}
