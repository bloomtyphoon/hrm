using HRM.BuildingBlocks.Application.Abstractions.Queries;

namespace HRM.Modules.Attendance.Application.Queries.GetLeaveTypes;

public sealed record GetLeaveTypesQuery : IQuery<IReadOnlyList<LeaveTypeDto>>
{
    public bool? IsActive { get; init; }
}
