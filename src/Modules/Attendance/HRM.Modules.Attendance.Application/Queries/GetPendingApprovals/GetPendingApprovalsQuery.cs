using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.BuildingBlocks.Application.Pagination;

namespace HRM.Modules.Attendance.Application.Queries.GetPendingApprovals;

public sealed record GetPendingApprovalsQuery : IQuery<PagedResult<PendingApprovalDto>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
