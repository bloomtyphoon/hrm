using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;

namespace HRM.Modules.Identity.Application.Commands.RevokeAllSessionsExceptCurrent;

/// <summary>
/// Command to revoke all sessions except current device.
/// Useful for security: "Logout all other devices"
///
/// Use Cases:
/// - Suspected account compromise
/// - Lost device - secure account by logging out all others
/// - Privacy - clear all old sessions
///
/// Security:
/// - Current token required (cannot accidentally logout self)
/// - AccountId from authenticated context
/// - IP tracking for audit trail (auto-injected by AuditBehavior)
///
/// Usage (API):
/// <code>
/// POST /api/identity/sessions/revoke-all-except-current
/// Authorization: Bearer {access_token}
/// Cookie: refreshToken={refresh_token}
///
/// Response (200 OK):
/// {
///   "message": "3 sessions were terminated",
///   "revokedCount": 3
/// }
/// </code>
/// </summary>
/// <param name="AccountId">Current account ID (from auth context)</param>
/// <param name="CurrentRefreshToken">Current refresh token to preserve</param>
public sealed record RevokeAllSessionsExceptCurrentCommand(
    Guid AccountId,
    string CurrentRefreshToken
) : IModuleCommand<RevokeAllSessionsResult>, IAuditableCommand
{
    public string ModuleName => "Identity";
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}

/// <summary>
/// Result DTO for RevokeAllSessionsExceptCurrent.
/// </summary>
public sealed record RevokeAllSessionsResult
{
    public required int RevokedCount { get; init; }
    public required string Message { get; init; }
}
