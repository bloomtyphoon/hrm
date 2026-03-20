using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.Modules.Attendance.Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Attendance.Application.Queries.GetLeaveTypes;

public sealed class GetLeaveTypesQueryHandler
    : IQueryHandler<GetLeaveTypesQuery, IReadOnlyList<LeaveTypeDto>>
{
    private readonly IAttendanceQueryContext _context;

    public GetLeaveTypesQueryHandler(IAttendanceQueryContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<LeaveTypeDto>> Handle(
        GetLeaveTypesQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.LeaveTypes.AsNoTracking();

        if (request.IsActive.HasValue)
            query = query.Where(lt => lt.IsActive == request.IsActive.Value);

        return await query
            .OrderBy(lt => lt.Name)
            .Select(lt => new LeaveTypeDto
            {
                Id = lt.Id,
                Name = lt.Name,
                Description = lt.Description,
                DefaultDaysPerYear = lt.DefaultDaysPerYear,
                IsPaid = lt.IsPaid,
                IsActive = lt.IsActive
            })
            .ToListAsync(cancellationToken);
    }
}
