namespace HRM.Modules.Attendance.Application.Queries.GetLeaveTypes;

public sealed record LeaveTypeDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required int DefaultDaysPerYear { get; init; }
    public required bool IsPaid { get; init; }
    public required bool IsActive { get; init; }
}
