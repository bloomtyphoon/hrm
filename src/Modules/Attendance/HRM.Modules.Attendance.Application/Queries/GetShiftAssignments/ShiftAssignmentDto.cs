namespace HRM.Modules.Attendance.Application.Queries.GetShiftAssignments;

public sealed record ShiftAssignmentDto
{
    public required Guid Id { get; init; }
    public required Guid ShiftId { get; init; }
    public required string ShiftName { get; init; }
    public required Guid EmployeeId { get; init; }
    public required DateOnly EffectiveFrom { get; init; }
    public DateOnly? EffectiveTo { get; init; }
}
