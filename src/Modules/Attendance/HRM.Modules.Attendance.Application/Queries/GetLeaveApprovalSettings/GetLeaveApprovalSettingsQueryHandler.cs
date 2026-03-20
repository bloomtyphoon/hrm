using HRM.BuildingBlocks.Application.Abstractions.Queries;
using HRM.Modules.Attendance.Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore;

namespace HRM.Modules.Attendance.Application.Queries.GetLeaveApprovalSettings;

internal sealed class GetLeaveApprovalSettingsQueryHandler
    : IQueryHandler<GetLeaveApprovalSettingsQuery, LeaveApprovalSettingsDto>
{
    private readonly IAttendanceQueryContext _context;

    public GetLeaveApprovalSettingsQueryHandler(IAttendanceQueryContext context)
    {
        _context = context;
    }

    public async Task<LeaveApprovalSettingsDto> Handle(
        GetLeaveApprovalSettingsQuery request,
        CancellationToken cancellationToken)
    {
        var setting = await _context.LeaveApprovalSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (setting is null)
        {
            // Return defaults if no settings exist yet
            return new LeaveApprovalSettingsDto
            {
                RequiresApproval = true,
                MaxApprovalLevels = 1,
                AutoApproveIfDaysLessThanOrEqual = null,
                AllowSelfCancel = true,
                NotifyOnDecision = true
            };
        }

        return new LeaveApprovalSettingsDto
        {
            RequiresApproval = setting.RequiresApproval,
            MaxApprovalLevels = setting.MaxApprovalLevels,
            AutoApproveIfDaysLessThanOrEqual = setting.AutoApproveIfDaysLessThanOrEqual,
            AllowSelfCancel = setting.AllowSelfCancel,
            NotifyOnDecision = setting.NotifyOnDecision
        };
    }
}
