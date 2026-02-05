using HRM.BuildingBlocks.Application.Abstractions.Commands;
using HRM.BuildingBlocks.Domain.Abstractions.Results;

namespace HRM.Modules.Identity.Application.Commands.RevokeSession;

/// <summary>
/// Command to revoke a specific session (logout from specific device).
/// </summary>
public sealed record RevokeSessionCommand(
    Guid SessionId,
    Guid AccountId
) : IModuleCommand<Result>, IAuditableCommand
{
    public string ModuleName => "Identity";
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
