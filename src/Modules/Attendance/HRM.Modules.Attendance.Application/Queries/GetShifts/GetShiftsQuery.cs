using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Application.Pagination;

namespace HRM.Modules.Attendance.Application.Queries.GetShifts;

public sealed record GetShiftsQuery : IQuery<PagedResult<ShiftDto>>
{
    public bool? IsActive { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
