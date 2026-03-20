using HRM.BuildingBlocks.Application.Abstractions.Queries;

namespace HRM.Modules.Attendance.Application.Queries.GetLeaveApprovalSettings;

public sealed record GetLeaveApprovalSettingsQuery : IQuery<LeaveApprovalSettingsDto>;
