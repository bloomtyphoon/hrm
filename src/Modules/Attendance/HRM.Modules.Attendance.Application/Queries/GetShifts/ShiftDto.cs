namespace HRM.Modules.Attendance.Application.Queries.GetShifts;

public sealed record ShiftDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required TimeOnly StartTime { get; init; }
    public required TimeOnly EndTime { get; init; }
    public string? Description { get; init; }
    public required bool IsActive { get; init; }
    public Guid? CompanyId { get; init; }
}
