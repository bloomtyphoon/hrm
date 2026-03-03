using HRM.BuildingBlocks.Application.Abstractions.Queries;

namespace HRM.Modules.Attendance.Application.Queries.GetAttendanceById;

public sealed record GetAttendanceByIdQuery : IQuery<AttendanceRecordDetailDto?>
{
    public required Guid RecordId { get; init; }
}
